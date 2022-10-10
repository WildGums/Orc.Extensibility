namespace Orc.Extensibility
{
    using System;

    public interface IPluginInfoProvider
    {
        IPluginInfo GetPluginInfo(string location, Type type);
    }
}
