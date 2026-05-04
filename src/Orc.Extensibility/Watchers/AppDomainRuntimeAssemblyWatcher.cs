namespace Orc.Extensibility;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Catel;
using Catel.IoC;
using Catel.Logging;
using Catel.Reflection;
using Catel.Services;
using MethodTimer;
using Microsoft.Extensions.Logging;
using Orc.FileSystem;

public class AppDomainRuntimeAssemblyWatcher : IInitializeAtStartup
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(AppDomainRuntimeAssemblyWatcher));

    private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;
    private readonly IAppDataService _appDataService;
    private readonly IDirectoryService _directoryService;
    private readonly IFileService _fileService;
    private readonly HashSet<string> _registeredLoadContexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Assembly> _loadedManagedAssembliesByName = new ConcurrentDictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, IntPtr> _loadedUnmanagedAssemblies = new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failedAssembliesByName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private IPluginLoadContext? _activeSingleLoadContext;

    public AppDomainRuntimeAssemblyWatcher(IRuntimeAssemblyResolverService runtimeAssemblyResolverService,
        IAppDataService appDataService, IDirectoryService directoryService, IFileService fileService)
    {
        ArgumentNullException.ThrowIfNull(runtimeAssemblyResolverService);
        ArgumentNullException.ThrowIfNull(appDataService);
        ArgumentNullException.ThrowIfNull(directoryService);
        ArgumentNullException.ThrowIfNull(fileService);

        _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
        _appDataService = appDataService;
        _directoryService = directoryService;
        _fileService = fileService;

        LoadedAssemblies = new List<IRuntimeAssembly>();
        AllowAssemblyResolvingFromOtherLoadContexts = true;
    }

    public event EventHandler<RuntimeLoadingAssemblyEventArgs>? AssemblyLoading;

    public event EventHandler<RuntimeLoadedAssemblyEventArgs>? AssemblyLoaded;

    /// <summary>
    /// Gets or sets a value whether assembly resolving from other load contexts is permitted.
    /// <para />
    /// This value should be enabled when multiple plugins can have dependencies on other plugins. Otherwise
    /// it should be disabled.
    /// </summary>
    public bool AllowAssemblyResolvingFromOtherLoadContexts { get; set; }

    public List<IRuntimeAssembly> LoadedAssemblies { get; private set; }

    public void Attach()
    {
        Attach(AssemblyLoadContext.Default);
    }

    public void Attach(AssemblyLoadContext assemblyLoadContext)
    {
        ArgumentNullException.ThrowIfNull(assemblyLoadContext);

        var name = assemblyLoadContext.Name ?? string.Empty;

        if (_registeredLoadContexts.Contains(name))
        {
            return;
        }

        _registeredLoadContexts.Add(name);

        Logger.LogDebug("Registering additional assembly load context '{Name}' to resolve runtime references", name);

        assemblyLoadContext.Resolving += OnLoadContextResolving;
        assemblyLoadContext.ResolvingUnmanagedDll += OnLoadContextResolvingUnmanagedDll;
    }

    private Assembly? OnLoadContextResolving(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyLoadContext);
        ArgumentNullException.ThrowIfNull(assemblyName);

        return LoadManagedAssembly(assemblyLoadContext, assemblyName, assemblyName.FullName);
    }

    [Time("{assemblyFullName}")]
    internal Assembly? LoadManagedAssembly(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName, string assemblyFullName)
    {
        ArgumentNullException.ThrowIfNull(assemblyLoadContext);
        ArgumentNullException.ThrowIfNull(assemblyName);
        ArgumentNullException.ThrowIfNull(assemblyFullName);

        var assemblyNameAsString = assemblyName.ToString();

        if (_failedAssembliesByName.Contains(assemblyNameAsString))
        {
            return null;
        }

        // Super fast way out
        if (_loadedManagedAssembliesByName.TryGetValue(assemblyNameAsString, out var existingAssembly))
        {
            return existingAssembly;
        }

        Logger.LogDebug("Requesting to load '{AssemblyName}'", assemblyName.FullName);

        // Load context, ignore the requesting assembly for now
        if (!string.IsNullOrWhiteSpace(assemblyName.Name))
        {
            IRuntimeAssembly? runtimeReference = null;

            var loadContexts = _runtimeAssemblyResolverService.GetPluginLoadContexts().ToList();

            if (!AllowAssemblyResolvingFromOtherLoadContexts)
            {
                if (_activeSingleLoadContext is null)
                {
                    Logger.LogDebug("Single load context is enabled, trying to find the current load context");

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
                                Logger.LogDebug("Found load context, caching result for all future assembly load actions to single load context of '{LoadContext}'", loadContext);

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

            var isResourcesAssembly = assemblyName.Name.EndsWithIgnoreCase(".resources");
            var culture = assemblyName.CultureInfo;

            // Special case for resource assemblies: respect the current culture
            if (isResourcesAssembly)
            {
                // Step 1: try specific culture (nl-NL)
                // Step 2: try larger culture (nl)
                while (culture is not null && !string.IsNullOrWhiteSpace(culture.Name))
                {
                    var locationWithBackslash = $"{culture.Name}\\{assemblyName.Name}.dll";
                    var locationWithForwardslash = $"{culture.Name}/{assemblyName.Name}.dll";

                    runtimeReference = (from pluginLoadContext in loadContexts
                                        from reference in pluginLoadContext.RuntimeAssemblies
                                        let costuraEmbeddedRuntimeAssembly = reference as ICosturaRuntimeAssembly
                                        where costuraEmbeddedRuntimeAssembly is not null &&
                                              costuraEmbeddedRuntimeAssembly.RelativeFileName.ContainsIgnoreCase(locationWithBackslash) ||
                                              costuraEmbeddedRuntimeAssembly.RelativeFileName.ContainsIgnoreCase(locationWithForwardslash)
                                        select reference).FirstOrDefault();
                    if (runtimeReference is not null)
                    {
                        break;
                    }

                    culture = culture.Parent;
                }
            }

            if (runtimeReference is null)
            {
                if (isResourcesAssembly)
                {
                    Logger.LogDebug("Could not provide resource assembly for '{AssemblyName}'", assemblyName.FullName);

                    // Don't try again
                    _failedAssembliesByName.Add(assemblyNameAsString);

                    return null;
                }

                runtimeReference = (from pluginLoadContext in loadContexts
                                    from reference in pluginLoadContext.RuntimeAssemblies
                                    where reference.Name.EqualsIgnoreCase(assemblyName.Name)
                                    select reference).FirstOrDefault();
            }

            if (runtimeReference is not null)
            {
                Logger.LogDebug("Trying to provide '{RuntimeReference}' as resolution for '{AssemblyName}'", runtimeReference, assemblyName.FullName);

                var error = string.Empty;

                try
                {
                    var assemblyLoadingEventArgs = new RuntimeLoadingAssemblyEventArgs(assemblyName, runtimeReference);

                    AssemblyLoading?.Invoke(this, assemblyLoadingEventArgs);

                    if (assemblyLoadingEventArgs.Cancel)
                    {
                        // Note: was explicitly canceled, don't add to ignore list
                        Logger.LogDebug("Canceling loading of '{RuntimeReference}' as resolution for '{AssemblyName}'", runtimeReference, assemblyName.FullName);
                        return null;
                    }

                    if (!runtimeReference.IsLoaded)
                    {
                        Assembly? loadedAssembly = null;

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
                                             where x.GetName().Name?.EqualsIgnoreCase(assemblyName.Name) ?? false
                                             select x).FirstOrDefault();
                if (alreadyLoadedAssembly is not null)
                {
                    Logger.LogWarning("Failed to load assembly from '{AssemblyFullName}', a different version '{Version}' is already loaded, returning already loaded assembly", assemblyFullName, alreadyLoadedAssembly.Version());

                    return alreadyLoadedAssembly;
                }
                else
                {
                    Logger.LogError("Failed to load assembly from '{AssemblyFullName}': {Error}", assemblyFullName, error);
                }
            }
        }

        _failedAssembliesByName.Add(assemblyNameAsString);

        return null;
    }

    [Time("{libraryName}")]
    internal IntPtr OnLoadContextResolvingUnmanagedDll(Assembly assembly, string libraryName)
    {
        Logger.LogDebug("Requesting to load unmanaged '{LibraryName}', requested by '{AssemblyName}'", libraryName, assembly.FullName);

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

            Logger.LogDebug("Trying to provide '{RuntimeReference}' as resolution for '{LibraryName}', temp file is '{TargetFileName}'", runtimeReference, libraryName, targetFileName);

            // Only load what we extracted ourselves and immediately took into use (blocked)
            if (!_loadedUnmanagedAssemblies.ContainsKey(targetFileName))
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
            _loadedUnmanagedAssemblies[targetFileName] = loadedAssembly;

            return loadedAssembly;
        }

        return IntPtr.Zero;
    }

    public void Initialize()
    {
        Attach();
    }
}
