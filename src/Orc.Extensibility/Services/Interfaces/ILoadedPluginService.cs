namespace Orc.Extensibility;

using System;
using System.Collections.Generic;

public interface ILoadedPluginService
{
    event EventHandler<PluginEventArgs>? PluginLoaded;

    List<IPluginInfo> GetLoadedPlugins();

    void AddPlugin(IPluginInfo pluginInfo);
}