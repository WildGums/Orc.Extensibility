namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using Catel;
    using Catel.Logging;

    using System.Runtime.Loader;
    using System.IO;
    using System.Reflection;
    using System.Linq;
    using Catel.Reflection;
    using System.Globalization;
    using MethodTimer;
    using Catel.Services;
    using Orc.FileSystem;

    public class AppDomainRuntimeAssemblyWatcher
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;
        private readonly IAppDataService _appDataService;
        private readonly IDirectoryService _directoryService;
        private readonly IFileService _fileService;
        private readonly HashSet<string> _registeredLoadContexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _loadedUmanagedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private PluginLoadContext _activeSingleLoadContext;

        public AppDomainRuntimeAssemblyWatcher(IRuntimeAssemblyResolverService runtimeAssemblyResolverService,
            IAppDataService appDataService, IDirectoryService directoryService, IFileService fileService)
        {
            Argument.IsNotNull(() => runtimeAssemblyResolverService);
            Argument.IsNotNull(() => appDataService);
            Argument.IsNotNull(() => directoryService);
            Argument.IsNotNull(() => fileService);

            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
            _appDataService = appDataService;
            _directoryService = directoryService;
            _fileService = fileService;

            LoadedAssemblies = new List<RuntimeAssembly>();
            AllowAssemblyResolvingFromOtherLoadContexts = true;
        }

        public event EventHandler<RuntimeLoadedAssemblyEventArgs> AssemblyLoaded;

        /// <summary>
        /// Gets or sets a value whether assembly resolving from other load contexts is permitted.
        /// <para />
        /// This value should be enabled when multiple plugins can have dependencies on other plugins. Otherwise
        /// it should be disabled.
        /// </summary>
        public bool AllowAssemblyResolvingFromOtherLoadContexts { get; set; }

        public List<RuntimeAssembly> LoadedAssemblies { get; private set; }

        public void Attach()
        {
            Attach(AssemblyLoadContext.Default);
        }

        public void Attach(AssemblyLoadContext assemblyLoadContext)
        {
            var name = assemblyLoadContext.Name;

            if (_registeredLoadContexts.Contains(name))
            {
                return;
            }

            _registeredLoadContexts.Add(name);

            Log.Debug($"Registering additional assembly load context '{name}' to resolve runtime references");

            assemblyLoadContext.Resolving += OnLoadContextResolving;
            assemblyLoadContext.ResolvingUnmanagedDll += OnLoadContextResolvingUnmanagedDll;
        }

        private Assembly OnLoadContextResolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            return LoadManagedAssembly(arg1, arg2, arg2.FullName);
        }

        [Time("{assemblyFullName}")]
        private Assembly LoadManagedAssembly(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName, string assemblyFullName)
        {
            Log.Debug($"Requesting to load '{assemblyName.FullName}'");

            // Load context, ignore the requesting assembly for now
            if (!string.IsNullOrWhiteSpace(assemblyName.Name))
            {
                RuntimeAssembly runtimeReference = null;

                var loadContexts = _runtimeAssemblyResolverService.GetPluginLoadContexts().ToList();

                if (!AllowAssemblyResolvingFromOtherLoadContexts)
                {
                    if (_activeSingleLoadContext is null)
                    {
                        Log.Debug($"Single load context is enabled, trying to find the current load context");

                        foreach (var loadContext in loadContexts)
                        {
                            var valid = false;
                            var pluginLocation = loadContext.PluginRuntimeAssembly.Source;

                            // Important: the plugin is probably the last loaded assembly, load descending
                            var assemblies = assemblyLoadContext.Assemblies.Where(p => !p.IsDynamic).ToList();

                            for (var i = assemblies.Count - 1; i >= 0; i--)
                            {
                                var potentialPluginAssembly = assemblies[i];
                                if (potentialPluginAssembly.Location.EqualsIgnoreCase(pluginLocation))
                                {
                                    Log.Debug($"Found load context, caching result for all future assembly load actions to single load context of '{loadContext}'");

                                    _activeSingleLoadContext = loadContext;
                                    valid = true;
                                    break;
                                }
                            }

                            if (valid)
                            {
                                break;
                            }
                        }
                    }

                    var activeSingleLoadContext = _activeSingleLoadContext;
                    if (activeSingleLoadContext is not null)
                    {
                        loadContexts.Clear();
                        loadContexts.Add(activeSingleLoadContext);
                    }
                }

                // Special case for resource assemblies: respect the current culture
                if (assemblyName.Name.EndsWithIgnoreCase(".resources"))
                {
                    var culture = assemblyName.CultureInfo ?? CultureInfo.CurrentUICulture;

                    // Step 1: try specific culture (nl-NL)
                    // Step 2: try larger culture (nl)
                    while (culture is not null && !string.IsNullOrWhiteSpace(culture.Name))
                    {
                        var locationWithBackslash = $"{culture.Name}\\{assemblyName.Name}.dll";
                        var locationWithForwardslash = $"{culture.Name}/{assemblyName.Name}.dll";

                        runtimeReference = (from pluginLoadContext in loadContexts
                                            from reference in pluginLoadContext.RuntimeAssemblies
                                            where reference.Name.ContainsIgnoreCase(locationWithBackslash) ||
                                                  reference.Name.ContainsIgnoreCase(locationWithForwardslash)
                                            select reference).FirstOrDefault();
                        if (runtimeReference is not null)
                        {
                            break;
                        }

                        culture = culture.Parent;
                    }

                    //var location = $"{culture.}/{arg2.Name}.resources.dll";
                }

                if (runtimeReference is null)
                {
                    runtimeReference = (from pluginLoadContext in loadContexts
                                        from reference in pluginLoadContext.RuntimeAssemblies
                                        where reference.Name.EqualsIgnoreCase(assemblyName.Name)
                                        select reference).FirstOrDefault();
                }

                if (runtimeReference is not null)
                {
                    Log.Debug($"Trying to provide '{runtimeReference}' as resolution for '{assemblyName.FullName}'");

                    var error = string.Empty;

                    try
                    {
                        if (!runtimeReference.IsLoaded)
                        {
                            Assembly loadedAssembly = null;

                            using (var stream = runtimeReference.GetStream())
                            {
                                loadedAssembly = assemblyLoadContext.LoadFromStream(stream);
                            }

                            runtimeReference.MarkLoaded();

                            LoadedAssemblies.Add(runtimeReference);

                            AssemblyLoaded?.Invoke(this, new RuntimeLoadedAssemblyEventArgs(assemblyName, runtimeReference, loadedAssembly));

                            return loadedAssembly;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Allow first attempt to fail
                        error = ex.Message;
                    }

                    // Fallback mechanism
                    var alreadyLoadedAssembly = (from x in AppDomain.CurrentDomain.GetLoadedAssemblies()
                                          where x.GetName().Name.EqualsIgnoreCase(assemblyName.Name)
                                          select x).FirstOrDefault();
                    if (alreadyLoadedAssembly is not null)
                    {
                        //Log.Error(ex, $"Failed to load assembly from '{runtimeReference}', a different version '{alreadyLoadedAssembly.Version()}' is already loaded");
                        return alreadyLoadedAssembly;
                    }
                    else
                    {
                        Log.Error($"Failed to load assembly from '{runtimeReference}': {error}");
                    }

                }
            }

            return null;
        }

        [Time("{libraryName}")]
        private IntPtr OnLoadContextResolvingUnmanagedDll(Assembly assembly, string libraryName)
        {
            Log.Debug($"Requesting to load '{libraryName}', requested by '{assembly.FullName}'");

            // Load context, ignore the requesting assembly for now
            var runtimeReference = (from pluginLoadContext in _runtimeAssemblyResolverService.GetPluginLoadContexts()
                                    from reference in pluginLoadContext.RuntimeAssemblies
                                    where Path.GetFileName(reference.Name).EqualsIgnoreCase(libraryName) ||
                                          Path.GetFileNameWithoutExtension(reference.Name).EqualsIgnoreCase(libraryName)
                                    select reference).FirstOrDefault();
            if (runtimeReference is not null)
            {
                // Note: unmanaged assemblies *must* be loaded from disk

                var targetDirectory = System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(Catel.IO.ApplicationDataTarget.UserLocal), 
                    "runtime", runtimeReference.Checksum);
                _directoryService.Create(targetDirectory);

                var targetFileName = Path.Combine(targetDirectory, runtimeReference.Name);

                Log.Debug($"Trying to provide '{runtimeReference}' as resolution for '{libraryName}', temp file is '{targetFileName}'");

                // Only load what we extracted ourselves and immediately took into use (blocked)
                if (!_loadedUmanagedAssemblies.Contains(targetFileName))
                {
                    if (!runtimeReference.IsLoaded)
                    {
                        // Note: maybe we could optimize by checking the hash? Or maybe just writing is faster than checking
                        using (var sourceStream = runtimeReference.GetStream())
                        {
                            using (var targetStream = _fileService.Create(targetFileName))
                            {
                                sourceStream.CopyTo(targetStream);
                                targetStream.Flush();
                            }
                        }

                        runtimeReference.MarkLoaded();
                    }
                }

                // In very rare cases, this could not work, see https://github.com/dotnet/runtime/issues/13819
                var loadedAssembly = System.Runtime.InteropServices.NativeLibrary.Load(targetFileName);

                // Only ones we have loaded the assembly, we are sure we don't want to overwrite it again
                _loadedUmanagedAssemblies.Add(targetFileName);

                return loadedAssembly;
            }

            return IntPtr.Zero;
        }
    }
}
