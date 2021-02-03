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
#endif

    public class AppDomainRuntimeAssemblyWatcher
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;
        private readonly HashSet<string> _registeredLoadContexts = new HashSet<string>();

        public AppDomainRuntimeAssemblyWatcher(IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        {
            Argument.IsNotNull(() => runtimeAssemblyResolverService);

            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;

            LoadedAssemblies = new List<RuntimeAssembly>();
        }

        public event EventHandler<RuntimeLoadedAssemblyEventArgs> AssemblyLoaded;

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
            Log.Debug($"Requesting to load '{arg2.FullName}'");

            // Load context, ignore the requesting assembly for now
            if (!string.IsNullOrWhiteSpace(arg2.Name))
            {
                RuntimeAssembly runtimeReference = null;

                // Special case for resource assemblies: respect the current culture
                if (arg2.Name.EndsWithIgnoreCase(".resources"))
                {
                    var culture = arg2.CultureInfo ?? CultureInfo.CurrentUICulture;

                    // Step 1: try specific culture (nl-NL)
                    // Step 2: try larger culture (nl)
                    while (culture is not null && !string.IsNullOrWhiteSpace(culture.Name))
                    {
                        var locationWithBackslash = $"{culture.Name}\\{arg2.Name}.dll";
                        var locationWithForwardslash = $"{culture.Name}/{arg2.Name}.dll";

                        runtimeReference = (from pluginLoadContext in _runtimeAssemblyResolverService.GetPluginLoadContexts()
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
                    runtimeReference = (from pluginLoadContext in _runtimeAssemblyResolverService.GetPluginLoadContexts()
                                        from reference in pluginLoadContext.RuntimeAssemblies
                                        where reference.Name.EqualsIgnoreCase(arg2.Name)
                                        select reference).FirstOrDefault();
                }

                if (runtimeReference is not null)
                {
                    Log.Debug($"Trying to provide '{runtimeReference.Location}' as resolution for '{arg2.FullName}'");

                    try
                    {
                        var loadedAssembly = Assembly.LoadFrom(runtimeReference.Location);

                        LoadedAssemblies.Add(runtimeReference);

                        AssemblyLoaded?.Invoke(this, new RuntimeLoadedAssemblyEventArgs(arg2, runtimeReference, loadedAssembly));

                        return loadedAssembly;
                    }
                    catch (Exception ex)
                    {
                        var loadedAssembly = (from x in AppDomain.CurrentDomain.GetLoadedAssemblies()
                                              where x.GetName().Name.EqualsIgnoreCase(arg2.Name)
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
