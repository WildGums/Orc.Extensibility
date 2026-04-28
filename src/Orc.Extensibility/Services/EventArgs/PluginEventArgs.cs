namespace Orc.Extensibility;

using System;

public class PluginEventArgs : EventArgs
{
    public PluginEventArgs(IPlugin plugin, string messageTitle, string messageDetails)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        PluginName = plugin.Info.Name;
        PluginInfo = plugin.Info;
        MessageTitle = messageTitle;
        MessageDetails = messageDetails;
    }

    public PluginEventArgs(string pluginName, string messageTitle, string messageDetails)
    {
        PluginName = pluginName;
        MessageTitle = messageTitle;
        MessageDetails = messageDetails;
    }

    public string PluginName { get; private set; }

    public IPluginInfo? PluginInfo { get; private set; }

    public string MessageTitle { get; private set; }

    public string MessageDetails { get; set; }
}
