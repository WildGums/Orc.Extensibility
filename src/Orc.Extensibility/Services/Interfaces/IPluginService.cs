namespace Orc.Extensibility;

using System;

public interface IPluginService
{
    event EventHandler<PluginEventArgs>? PluginLoadingFailed;

    event EventHandler<PluginEventArgs>? PluginLoaded;
}