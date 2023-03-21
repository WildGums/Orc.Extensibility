namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
using Catel.Logging;

public class LoadedPluginService : ILoadedPluginService
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, IPluginInfo> _loadedPlugins = new();

       
    public List<IPluginInfo> GetLoadedPlugins()
    {
        lock (_loadedPlugins)
        {
            return _loadedPlugins.Values.ToList();
        }
    }

    public event EventHandler<PluginEventArgs>? PluginLoaded;

    public void AddPlugin(IPluginInfo pluginInfo)
    {
        ArgumentNullException.ThrowIfNull(pluginInfo);

        Log.Debug($"Registering plugin '{pluginInfo}' as loaded");

        lock (_loadedPlugins)
        {
            var key = pluginInfo.FullTypeName.ToLower();
            if (_loadedPlugins.ContainsKey(key))
            {
                Log.Warning($"Plugin '{pluginInfo}' is already marked as loaded");
                return;
            }

            _loadedPlugins.Add(key, pluginInfo);
        }

        PluginLoaded?.Invoke(this, new PluginEventArgs(pluginInfo, string.Empty, string.Empty));
    }
}
