namespace Orc.Extensibility
{
    using System;
    using System.Reflection.Metadata;

    public class PluginInfoProvider : IPluginInfoProvider
    {
        public PluginInfoProvider()
        {
        }

        public virtual IPluginInfo GetPluginInfo(string location, MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            return new PluginInfo(location, metadataReader, typeDefinition);
        }
    }
}
