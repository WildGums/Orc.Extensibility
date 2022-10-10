namespace Orc.Extensibility
{
    using System;

    public class PluginInfoProvider : IPluginInfoProvider
    {
        public PluginInfoProvider()
        {
        }

        public virtual IPluginInfo GetPluginInfo(string location, Type type)
        {
            ArgumentNullException.ThrowIfNull(location);
            ArgumentNullException.ThrowIfNull(type);

            return new PluginInfo(location, type);
        }
    }
}
