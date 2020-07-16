namespace Orc.Extensibility
{
    using System;
    using System.Reflection.Metadata;

    public class PluginInfoProvider : IPluginInfoProvider
    {
        public PluginInfoProvider()
        {
        }

        public virtual IPluginInfo GetPluginInfo(string location, Type type)
        {
            return new PluginInfo(location, type);
        }
    }
}
