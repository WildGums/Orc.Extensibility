namespace Orc.Extensibility
{
    using System.Reflection.Metadata;

    public interface IPluginInfoProvider
    {
        IPluginInfo GetPluginInfo(string location, MetadataReader metadataReader, TypeDefinition typeDefinition);
    }
}
