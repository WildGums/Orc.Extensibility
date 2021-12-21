namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using Catel;
    using Catel.Logging;

#if NETCORE
    using System.Runtime.Loader;
    using System.IO;
    using System.Reflection;
    using System.Linq;
    using Catel.Reflection;
    using System.Globalization;
    using MethodTimer;
#endif

    public class AppDomainRuntimeAssemblyWatcher
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;
        private readonly HashSet<string> _registeredLoadContexts = new HashSet<string>();

        private PluginLoadContext _activeSingleLoadContext;

        public AppDomainRuntimeAssemblyWatcher(IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        {
            Argument.IsNotNull(() => runtimeAssemblyResolverService);

            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;

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
#if NETCORE
            Attach(AssemblyLoadContext.Default);
#endif
        }

#if NETCORE
        public void Attach(AssemblyLoadContext assemblyLoadContext)
        {
            var name = assemblyLoadContext.Name;

            if (_registeredLoadContexts.Contains(name))
            {
                return;
            }

            _registeredLoadContexts.Add(name);

            var targetDirectory = _runtimeAssemblyResolverService.TargetDirectory;

            Log.Debug($"Registering '{targetDirectory}' as extra path to resolve runtime references for assembly load context '{name}'");

            assemblyLoadContext.Resolving += OnLoadContextResolving;
            assemblyLoadContext.ResolvingUnmanagedDll += OnLoadContextResolvingUnmanagedDll;
        }
#endif

#if NETCORE
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
                            var pluginLocation = loadContext.PluginLocation;

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
                                            where reference.Location.ContainsIgnoreCase(locationWithBackslash) ||
                                                  reference.Location.ContainsIgnoreCase(locationWithForwardslash)
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
                    Log.Debug($"Trying to provide '{runtimeReference.Location}' as resolution for '{assemblyName.FullName}'");

                    try
                    {
                        var loadedAssembly = Assembly.LoadFrom(runtimeReference.Location);

                        LoadedAssemblies.Add(runtimeReference);

                        AssemblyLoaded?.Invoke(this, new RuntimeLoadedAssemblyEventArgs(assemblyName, runtimeReference, loadedAssembly));

                        return loadedAssembly;
                    }
                    catch (Exception ex)
                    {
                        var loadedAssembly = (from x in AppDomain.CurrentDomain.GetLoadedAssemblies()
                                              where x.GetName().Name.EqualsIgnoreCase(assemblyName.Name)
                                              select x).FirstOrDefault();
                        if (loadedAssembly is not null)
                        {
                            Log.Error(ex, $"Failed to load assembly from '{runtimeReference.Location}', a different version '{loadedAssembly.Version()}' is already loaded");
                        }
                        else
                        {
                            Log.Error(ex, $"Failed to load assembly from '{runtimeReference.Location}'");
                        }

                        throw;
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
                                    where Path.GetFileName(reference.Location).EqualsIgnoreCase(libraryName) ||
                                          Path.GetFileNameWithoutExtension(reference.Location).EqualsIgnoreCase(libraryName)
                                    select reference).FirstOrDefault();
            if (runtimeReference is not null)
            {
                Log.Debug($"Trying to provide '{runtimeReference.Location}' as resolution for '{libraryName}'");

                // In very rare cases, this could not work, see https://github.com/dotnet/runtime/issues/13819
                var loadedAssembly = System.Runtime.InteropServices.NativeLibrary.Load(runtimeReference.Location);
                return loadedAssembly;
            }

            return IntPtr.Zero;
        }
#endif
    }
}
