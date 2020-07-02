namespace Orc.Extensibility
{
    using System;
    using System.Reflection.Metadata;

    public interface IPluginInfoProvider
    {
        IPluginInfo GetPluginInfo(string location, Type type);
    }
}
