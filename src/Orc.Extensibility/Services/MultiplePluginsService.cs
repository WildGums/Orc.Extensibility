namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catel.Logging;
using MethodTimer;
using Microsoft.Extensions.Logging;

public class MultiplePluginsService : IMultiplePluginsService
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(MultiplePluginsService));

    private readonly IPluginFactory _pluginFactory;
    private readonly ILoadedPluginService _loadedPluginService;
    private readonly IPluginManager _pluginManager;

    public MultiplePluginsService(IPluginManager pluginManager, IPluginFactory pluginFactory, 
        ILoadedPluginService loadedPluginService)
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

        Logger.LogInformation("Found '{0}' plugins", plugins.Count());

        var pluginsToLoad = new Queue<IPluginInfo>();

        foreach (var plugin in plugins)
        {
            Logger.LogInformation("  * {0} ({1})", plugin, plugin.Location);

            if (requestedPlugins.Length == 0 || requestedPlugins.Contains(plugin.Plugin.FullTypeName))
            {
                pluginsToLoad.Enqueue(plugin);
            }
        }

        var pluginTryCount = new Dictionary<string, int>();
        var pluginInstances = new List<Plugin>();

        while (pluginsToLoad.Count > 0)
        {
            var pluginToLoad = pluginsToLoad.Dequeue();

            if (!pluginTryCount.ContainsKey(pluginToLoad.Plugin.FullTypeName))
            {
                pluginTryCount[pluginToLoad.Plugin.FullTypeName] = 0;
            }

            pluginTryCount[pluginToLoad.Plugin.FullTypeName]++;
            var isLastRetry = pluginTryCount[pluginToLoad.Plugin.FullTypeName] == pluginsToLoad.Count;

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
            Logger.LogInformation("Instantiating plugin '{0}'", pluginToLoad.Plugin.FullTypeName);

            var pluginInstance = _pluginFactory.CreatePluginType(pluginToLoad.Plugin);
            var plugin = new Plugin(pluginInstance, pluginToLoad);

            _loadedPluginService.AddPlugin(plugin);

            PluginLoaded?.Invoke(this, new PluginEventArgs(plugin, "Loaded plugin", $"Plugin {pluginToLoad.Name} has been loaded and activated"));
                
            return plugin;
        }
        catch (Exception ex)
        {
            var message = $"Plugin '{pluginToLoad.Name}' could not be loaded, is last retry: '{isLastTry}'";

            Logger.LogWarning(ex, message);

            if (isLastTry)
            {
                PluginLoadingFailed?.Invoke(this, new PluginEventArgs(pluginToLoad.Name, "Failed to load plugin", message));
            }
                
            return null;
        }
    }
}
