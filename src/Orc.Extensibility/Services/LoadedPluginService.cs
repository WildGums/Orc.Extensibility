namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public class LoadedPluginService : ILoadedPluginService
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(LoadedPluginService));

    private readonly Dictionary<string, IPluginInfo> _loadedPlugins = new();

    public LoadedPluginService()
    {
        
    }

    public event EventHandler<PluginEventArgs>? PluginLoaded;

    public IReadOnlyList<IPluginInfo> GetLoadedPlugins()
    {
        lock (_loadedPlugins)
        {
            return _loadedPlugins.Values.ToArray();
        }
    }

    public void AddPlugin(IPluginInfo pluginInfo)
    {
        ArgumentNullException.ThrowIfNull(pluginInfo);

        Logger.LogDebug($"Registering plugin '{pluginInfo}' as loaded");

        lock (_loadedPlugins)
        {
            var key = pluginInfo.FullTypeName.ToLower();
            if (_loadedPlugins.ContainsKey(key))
            {
                Logger.LogWarning($"Plugin '{pluginInfo}' is already marked as loaded");
                return;
            }

            _loadedPlugins.Add(key, pluginInfo);
        }

        PluginLoaded?.Invoke(this, new PluginEventArgs(pluginInfo, string.Empty, string.Empty));
    }
}
