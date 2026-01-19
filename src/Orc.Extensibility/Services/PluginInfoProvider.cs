namespace Orc.Extensibility;

using System;

public class PluginInfoProvider : IPluginInfoProvider
{
    public virtual IPluginInfo GetPluginInfo(string location, Type type, Type? registrarType)
    {
        return new PluginInfo(location, type, registrarType);
    }
}
