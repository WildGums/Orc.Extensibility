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
            loadContext.ResolvingUnmanagedDll += OnLoadContextResolvingUnmanagedDll;

            //loadContext.
#endif

            AppDomain.CurrentDomain.AssemblyResolve += OnAppDomainAssemblyResolve;

        }

#if NETCORE
        private IntPtr ResolveNativeDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            return IntPtr.Zero;
        }

        private IntPtr OnLoadContextResolvingUnmanagedDll(Assembly arg1, string arg2)
        {
            //NativeLibrary.

            //NativeLibrary.SetDllImportResolver(, )

            //throw new NotImplementedException();
            return IntPtr.Zero;
        }
#endif

        private Assembly OnAppDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            var runtimeAssemblies = _runtimeAssemblyResolverService.GetRuntimeAssemblies();

            var assemblyPath = (from x in runtimeAssemblies
                                where x.Name.EqualsIgnoreCase(assemblyName.Name)
                                select x.Location).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                return null;
            }

            Log.Debug($"Dynamically lazy-loading runtime assembly '{args.Name}'");

            var assembly = Assembly.LoadFrom(assemblyPath);

//#if NETCORE
//            NativeLibrary.SetDllImportResolver(assembly, ResolveNativeDll);
//#endif

            return assembly;
        } 
    }
}
