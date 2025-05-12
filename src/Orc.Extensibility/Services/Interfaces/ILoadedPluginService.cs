namespace Orc.Extensibility;

using System;
using System.Collections.Generic;

public interface ILoadedPluginService
{
    event EventHandler<PluginEventArgs>? PluginLoaded;

    IReadOnlyList<IPluginInfo> GetLoadedPlugins();

    void AddPlugin(IPluginInfo pluginInfo);
}
