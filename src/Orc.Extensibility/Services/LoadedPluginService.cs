namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public class LoadedPluginService : ILoadedPluginService
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(LoadedPluginService));

    private readonly Dictionary<string, IPlugin> _loadedPlugins = new();

    public LoadedPluginService()
    {
        
    }

    public event EventHandler<PluginEventArgs>? PluginLoaded;

    public IReadOnlyList<IPlugin> GetLoadedPlugins()
    {
        lock (_loadedPlugins)
        {
            return _loadedPlugins.Values.ToArray();
        }
    }

    public void AddPlugin(IPlugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        Logger.LogDebug($"Registering plugin '{plugin}' as loaded");

        lock (_loadedPlugins)
        {
            var key = plugin.Info.Plugin.FullTypeName.ToLower();
            if (_loadedPlugins.ContainsKey(key))
            {
                Logger.LogWarning($"Plugin '{plugin}' is already marked as loaded");
                return;
            }

            _loadedPlugins.Add(key, plugin);
        }

        PluginLoaded?.Invoke(this, new PluginEventArgs(plugin, string.Empty, string.Empty));
    }
}
