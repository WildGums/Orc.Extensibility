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

            //foreach (var runtimeReference in _runtimeAssemblyResolverService.GetRuntimeAssemblies())
            //{
            //    if (runtimeReference.Name.EqualsIgnoreCase(arg2.Name))
            //    {
            //        var assembly = Assembly.LoadFrom(runtimeReference.Location);
            //        return assembly;
            //    }
            //}

            return null;
        }

        private IntPtr OnLoadContextResolvingUnmanagedDll(Assembly assembly, string libraryName)
        {
            Log.Debug($"Requesting to load '{libraryName}', requested by '{assembly.FullName}'");

            return IntPtr.Zero;
        }
#endif
    }
}
