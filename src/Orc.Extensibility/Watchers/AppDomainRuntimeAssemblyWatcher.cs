namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using Catel;
    using Catel.Logging;

#if NETCORE
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.IO;
    using System.Reflection;
    using System.Linq;
    using System.Linq.Expressions;
    using Catel.Reflection;
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
        }

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
                var runtimeReference = (from pluginLoadContext in _runtimeAssemblyResolverService.GetPluginLoadContexts()
                                        from reference in pluginLoadContext.RuntimeAssemblies
                                        where reference.Name.EqualsIgnoreCase(arg2.Name)
                                        select reference).FirstOrDefault();
                if (runtimeReference is null == false)
                {
                    Log.Debug($"Trying to provide '{runtimeReference.Location}' as resolution for '{arg2.FullName}'");

                    try
                    {
                        var loadedAssembly = Assembly.LoadFrom(runtimeReference.Location);
                        return loadedAssembly;
                    }
                    catch (Exception ex)
                    {
                        var loadedAssembly = (from x in AppDomain.CurrentDomain.GetLoadedAssemblies()
                                              where x.GetName().Name.EqualsIgnoreCase(arg2.Name)
                                              select x).FirstOrDefault();
                        if (loadedAssembly is null == false)
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
            if (runtimeReference is null == false)
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
