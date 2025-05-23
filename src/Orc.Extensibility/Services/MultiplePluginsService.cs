﻿namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catel.Logging;
using MethodTimer;

public class MultiplePluginsService : IMultiplePluginsService
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private readonly IPluginFactory _pluginFactory;
    private readonly ILoadedPluginService _loadedPluginService;
    private readonly IPluginManager _pluginManager;

    public MultiplePluginsService(IPluginManager pluginManager, IPluginFactory pluginFactory, ILoadedPluginService loadedPluginService)
    {
        ArgumentNullException.ThrowIfNull(pluginManager);
        ArgumentNullException.ThrowIfNull(pluginFactory);
        ArgumentNullException.ThrowIfNull(loadedPluginService);

        _pluginManager = pluginManager;
        _pluginFactory = pluginFactory;
        _loadedPluginService = loadedPluginService;
    }

    public event EventHandler<PluginEventArgs>? PluginLoadingFailed;

    public event EventHandler<PluginEventArgs>? PluginLoaded;

    /// <summary>
    /// Configures the and load plugins.
    /// </summary>
    /// <param name="requestedPlugins">The requested plugins.</param>
    /// <returns>IReadOnlyList&lt;IPlugin&gt;.</returns>
    [Time]
    public virtual async Task<IReadOnlyList<IPlugin>> ConfigureAndLoadPluginsAsync(params string[] requestedPlugins)
    {
        var plugins = await _pluginManager.RefreshAndGetPluginsAsync();

        Log.Info("Found '{0}' plugins", plugins.Count());

        var pluginsToLoad = new Queue<IPluginInfo>();

        foreach (var plugin in plugins)
        {
            Log.Info("  * {0} ({1})", plugin, plugin.Location);

            if (requestedPlugins.Length == 0 || requestedPlugins.Contains(plugin.FullTypeName))
            {
                pluginsToLoad.Enqueue(plugin);
            }
        }

        var pluginTryCount = new Dictionary<string, int>();
        var pluginInstances = new List<Plugin>();

        while (pluginsToLoad.Count > 0)
        {
            var pluginToLoad = pluginsToLoad.Dequeue();

            if (!pluginTryCount.ContainsKey(pluginToLoad.FullTypeName))
            {
                pluginTryCount[pluginToLoad.FullTypeName] = 0;
            }

            pluginTryCount[pluginToLoad.FullTypeName]++;
            var isLastRetry = pluginTryCount[pluginToLoad.FullTypeName] == pluginsToLoad.Count;

            var plugin = await ConfigureAndLoadPluginAsync(pluginToLoad, isLastRetry);
            if (plugin is null)
            {
                // Try again once other plugins have been loaded
                pluginsToLoad.Enqueue(pluginToLoad);
            }
            else
            {
                pluginInstances.Add(plugin);
            }
        }

        return pluginInstances;
    }
        
    protected virtual async Task<Plugin?> ConfigureAndLoadPluginAsync(IPluginInfo pluginToLoad, bool isLastTry)
    {
        try
        {
            Log.Info("Instantiating plugin '{0}'", pluginToLoad.FullTypeName);

            var pluginInstance = _pluginFactory.CreatePlugin(pluginToLoad);
            var plugin = new Plugin(pluginInstance, pluginToLoad);

            _loadedPluginService.AddPlugin(pluginToLoad);

            PluginLoaded?.Invoke(this, new PluginEventArgs(pluginToLoad, "Loaded plugin", $"Plugin {pluginToLoad.Name} has been loaded and activated"));
                
            return plugin;
        }
        catch (Exception ex)
        {
            var message = $"Plugin '{pluginToLoad.Name}' could not be loaded, is last retry: '{isLastTry}'";

            Log.Warning(ex, message);

            if (isLastTry)
            {
                PluginLoadingFailed?.Invoke(this, new PluginEventArgs(pluginToLoad, "Failed to load plugin", message));
            }
                
            return null;
        }
    }
}
