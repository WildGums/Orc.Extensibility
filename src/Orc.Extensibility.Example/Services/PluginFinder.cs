namespace Orc.Extensibility.Example.Services
{
    using System;
    using Catel;
    using Catel.Logging;
    using FileSystem;

    public class PluginFinder : Orc.Extensibility.PluginFinderBase
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public PluginFinder(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider, IPluginCleanupService pluginCleanupService,
            IDirectoryService directoryService, IFileService fileService, IAssemblyReflectionService assemblyReflectionService, IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
            : base(pluginLocationsProvider, pluginInfoProvider, pluginCleanupService, directoryService, fileService, assemblyReflectionService, runtimeAssemblyResolverService)
        {
        }

        protected override bool IsPlugin(PluginProbingContext context, Type type)
        {
            return type.ImplementsInterface<ICustomPlugin>();
        }

        protected override bool ShouldIgnoreAssembly(string assemblyPath)
        {
            // Since by default, the plugin finder ignores Orc.* assemblies, we need to override it here (ExtensionA and ExtensionB)
            if (assemblyPath.ContainsIgnoreCase("Orc.Extensibility.Example.Extension"))
            {
                return false;
            }

            return base.ShouldIgnoreAssembly(assemblyPath);
        }
    }
}
