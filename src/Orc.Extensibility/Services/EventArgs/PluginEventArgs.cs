namespace Orc.Extensibility;

using System;

public class PluginEventArgs : EventArgs
{
    public PluginEventArgs(IPluginInfo pluginInfo, string messageTitle, string messageDetails)
    {
        ArgumentNullException.ThrowIfNull(pluginInfo);

        PluginName = pluginInfo.Name;
        PluginInfo = pluginInfo;
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