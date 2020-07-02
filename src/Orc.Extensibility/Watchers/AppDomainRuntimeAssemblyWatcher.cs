namespace Orc.Extensibility
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Catel;
    using Catel.Logging;

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
            AppDomain.CurrentDomain.AssemblyResolve += OnAppDomainAssemblyResolve;
        }

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

            return Assembly.LoadFrom(assemblyPath);
        }
    }
}
