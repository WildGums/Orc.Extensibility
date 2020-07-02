namespace Orc.Extensibility.Example.Services
{
    using System;
    using System.Reflection.Metadata;
    using Catel;
    using Catel.Logging;
    using FileSystem;

    public class PluginFinder : Orc.Extensibility.PluginFinderBase
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public PluginFinder(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider,
            IPluginCleanupService pluginCleanupService, IDirectoryService directoryService, IFileService fileService)
            : base(pluginLocationsProvider, pluginInfoProvider, pluginCleanupService, directoryService, fileService)
        {
        }

        protected override bool IsPlugin(MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            return typeDefinition.ImplementsInterface<ICustomPlugin>(metadataReader);
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
