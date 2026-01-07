namespace Orc.Extensibility;

using System;
using System.Linq;
using System.Threading.Tasks;
using Catel;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public class SinglePluginService : ISinglePluginService
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(SinglePluginService));

    private readonly IPluginFactory _pluginFactory;
    private readonly ILoadedPluginService _loadedPluginService;
    private readonly IPluginManager _pluginManager;

    private IPluginInfo? _fallbackPlugin;

    public SinglePluginService(IPluginManager pluginManager, IPluginFactory pluginFactory, ILoadedPluginService loadedPluginService)
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

    public async Task<IPlugin?> ConfigureAndLoadPluginAsync(string expectedPlugin, string defaultPlugin)
    {
        var plugins = await _pluginManager.RefreshAndGetPluginsAsync();

        Logger.LogDebug("Found '{0}' plugins", plugins.Count());

        IPluginInfo? pluginToLoad = null;

        // Step 1: search for full name
        foreach (var plugin in plugins)
        {
            Logger.LogDebug("  * {0} ({1})", plugin, plugin.Location);

            if (plugin.FullTypeName.EqualsIgnoreCase(expectedPlugin))
            {
                Logger.LogDebug($"Found extension via full type name matching");

                pluginToLoad = plugin;
                break;
            }
        }

        // Step 2: allow plugin aliases
        if (pluginToLoad is null)
        {
            foreach (var plugin in plugins)
            {
                if (plugin.Aliases.Any(x => x.EqualsIgnoreCase(expectedPlugin)))
                {
                    Logger.LogDebug($"Found extension via alias '{expectedPlugin}'");

                    pluginToLoad = plugin;
                    break;
                }
            }
        }

        // Step 3: search for simplified name (only for single plugins)
        if (pluginToLoad is null)
        {
            foreach (var plugin in plugins)
            {
                if (plugin.FullTypeName.EndsWithIgnoreCase(expectedPlugin))
                {
                    Logger.LogDebug("Found extension by partial type name matching");

                    pluginToLoad = plugin;
                    break;
                }
            }
        }

        var fallbackPlugin = (from plugin in plugins
                                 where string.Equals(plugin.FullTypeName, defaultPlugin)
                                 select plugin).FirstOrDefault() ??
                             _fallbackPlugin;

        if (pluginToLoad is null)
        {
            const string message = "Plugin could not be found, using default plugin";

            Logger.LogWarning(message);

            PluginLoadingFailed?.Invoke(this, new PluginEventArgs(expectedPlugin, "Failed to load plugin", message));

            pluginToLoad = fallbackPlugin;
        }

        if (pluginToLoad is null)
        {
            return null;
        }

        object? pluginInstance = null;

        try
        {
            Logger.LogDebug("Instantiating plugin '{0}'", pluginToLoad.FullTypeName);

            pluginInstance = _pluginFactory.CreatePlugin(pluginToLoad);
        }
        catch (Exception ex)
        {
            var message = $"Plugin '{pluginToLoad.Name}' could not be loaded, falling back to default plugin";

            Logger.LogWarning(ex, message);

            PluginLoadingFailed?.Invoke(this, new PluginEventArgs(pluginToLoad.Name, "Failed to load plugin", message));

            if (fallbackPlugin is not null)
            {
                pluginToLoad = fallbackPlugin;

                Logger.LogDebug("Instantiating fallback plugin '{0}'", pluginToLoad.FullTypeName);

                pluginInstance = _pluginFactory.CreatePlugin(pluginToLoad);
            }
        }

        if (pluginInstance is null)
        {
            return null;
        }

        Logger.LogDebug($"Final instantiated plugin is '{pluginInstance.GetType().Name}'");

        _loadedPluginService.AddPlugin(pluginToLoad);

        PluginLoaded?.Invoke(this, new PluginEventArgs(pluginToLoad, "Loaded plugin", $"Plugin {pluginToLoad.Name} has been loaded and activated"));

        return new Plugin(pluginInstance, pluginToLoad);
    }

    public async Task SetFallbackPluginAsync(IPluginInfo? fallbackPlugin)
    {
        _fallbackPlugin = fallbackPlugin;
    }
}
