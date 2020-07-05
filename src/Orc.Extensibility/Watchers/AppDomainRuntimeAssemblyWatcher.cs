namespace Orc.Extensibility
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Catel;
    using Catel.Logging;

#if NETCORE
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.IO;
#endif

    public class AppDomainRuntimeAssemblyWatcher
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;

        public AppDomainRuntimeAssemblyWatcher(IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        {
            Argument.IsNotNull(() => runtimeAssemblyResolverService);

            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
        }

        public void Attach()
        {
#if NETCORE
            var targetDirectory = _runtimeAssemblyResolverService.TargetDirectory;

            Log.Debug($"Registering '{targetDirectory}' as extra path to resolve runtime references");

            var loadContext = AssemblyLoadContext.Default;
            loadContext.Resolving += OnLoadContextResolving;
            loadContext.ResolvingUnmanagedDll += OnLoadContextResolvingUnmanagedDll;
#endif
        }

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

                    return Assembly.LoadFrom(runtimeReference.Location);
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
                                    where Path.GetFileName(reference.Location).EqualsIgnoreCase(libraryName)
                                    select reference).FirstOrDefault();
            if (runtimeReference is null == false)
            {
                Log.Debug($"Trying to provide '{runtimeReference.Location}' as resolution for '{libraryName}'");

                // In very rare cases, this could not work, see https://github.com/dotnet/runtime/issues/13819
                return System.Runtime.InteropServices.NativeLibrary.Load(runtimeReference.Location);
            }

            return IntPtr.Zero;
        }
#endif
    }
}
