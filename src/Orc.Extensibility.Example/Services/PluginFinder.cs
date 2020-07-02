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

        private readonly string _pluginName = $"{typeof(ICustomPlugin).Namespace}.{typeof(ICustomPlugin).Name}";

        public PluginFinder(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider,
            IPluginCleanupService pluginCleanupService, IDirectoryService directoryService, IFileService fileService)
            : base(pluginLocationsProvider, pluginInfoProvider, pluginCleanupService, directoryService, fileService)
        {
        }

        protected override bool IsPlugin(MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            foreach (var interfaceHandle in typeDefinition.GetInterfaceImplementations())
            {
                try
                {
                    var interfaceImplementation = metadataReader.GetInterfaceImplementation(interfaceHandle);
                    var fullTypeName = string.Empty;

                    switch (interfaceImplementation.Interface.Kind)
                    {
                        case HandleKind.TypeDefinition:
                            var interfaceTypeDefinition = metadataReader.GetTypeDefinition((TypeDefinitionHandle)interfaceImplementation.Interface);
                            fullTypeName = interfaceTypeDefinition.GetFullTypeName(metadataReader);
                            break;

                        case HandleKind.TypeReference:
                            var interfaceTypeReference = metadataReader.GetTypeReference((TypeReferenceHandle)interfaceImplementation.Interface);
                            fullTypeName = interfaceTypeReference.GetFullTypeName(metadataReader);
                            break;
                    }

                    if (fullTypeName.Equals(_pluginName))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            return false;
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
