// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SinglePluginService.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility
{
    using System;
    using System.Linq;
    using Catel;
    using Catel.Logging;

    public class SinglePluginService : ISinglePluginService
    {
        #region Fields
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IPluginFactory _pluginFactory;
        private readonly ILoadedPluginService _loadedPluginService;
        private readonly IPluginManager _pluginManager;
        #endregion

        #region Constructors
        public SinglePluginService(IPluginManager pluginManager, IPluginFactory pluginFactory, ILoadedPluginService loadedPluginService)
        {
            Argument.IsNotNull(() => pluginManager);
            Argument.IsNotNull(() => pluginFactory);
            Argument.IsNotNull(() => loadedPluginService);

            _pluginManager = pluginManager;
            _pluginFactory = pluginFactory;
            _loadedPluginService = loadedPluginService;
        }
        #endregion

        #region Events
        public event EventHandler<PluginEventArgs> PluginLoadingFailed;

        public event EventHandler<PluginEventArgs> PluginLoaded;
        #endregion

        #region Methods
        public IPlugin ConfigureAndLoadPlugin(string expectedPlugin, string defaultPlugin)
        {
            var plugins = _pluginManager.GetPlugins();

            Log.Debug("Found '{0}' plugins", plugins.Count());

            IPluginInfo pluginToLoad = null;

            // Step 1: search for full name
            foreach (var plugin in plugins)
            {
                Log.Debug("  * {0} ({1})", plugin, plugin.Location);

                if (plugin.FullTypeName.EqualsIgnoreCase(expectedPlugin))
                {
                    pluginToLoad = plugin;
                    break;
                }
            }

            // Step 2: search for simplified name (only for single plugins)
            if (pluginToLoad == null)
            {
                foreach (var plugin in plugins)
                {
                    if (plugin.FullTypeName.EndsWithIgnoreCase(expectedPlugin))
                    {
                        pluginToLoad = plugin;
                        break;
                    }
                }
            }

            var fallbackPlugin = (from plugin in plugins
                                  where string.Equals(plugin.FullTypeName, defaultPlugin)
                                  select plugin).FirstOrDefault();

            if (pluginToLoad == null)
            {
                const string message = "Plugin could not be found, using default plugin";

                Log.Warning(message);

                PluginLoadingFailed.SafeInvoke(this, () => new PluginEventArgs(expectedPlugin, "Failed to load plugin", message));

                pluginToLoad = fallbackPlugin;
            }

            if (pluginToLoad == null)
            {
                return null;
            }

            object pluginInstance;

            try
            {
                Log.Debug("Instantiating plugin '{0}'", pluginToLoad.FullTypeName);

                pluginInstance = _pluginFactory.CreatePlugin(pluginToLoad);
            }
            catch (Exception ex)
            {
                var message = $"Plugin '{pluginToLoad.Name}' could not be loaded, falling back to default plugin";

                Log.Warning(ex, message);

                PluginLoadingFailed.SafeInvoke(this, () => new PluginEventArgs(pluginToLoad.Name, "Failed to load plugin", message));

                pluginToLoad = fallbackPlugin;

                Log.Debug("Instantiating fallback plugin '{0}'", pluginToLoad.FullTypeName);

                pluginInstance = _pluginFactory.CreatePlugin(pluginToLoad);
            }

            Log.Debug($"Final instantiated plugin is '{pluginInstance?.GetType().Name}'");

            _loadedPluginService.AddPlugin(pluginToLoad);

            PluginLoaded.SafeInvoke(this, () => new PluginEventArgs(pluginToLoad, "Loaded plugin", $"Plugin {pluginToLoad.Name} has been loaded and activated"));

            return new Plugin(pluginInstance, pluginToLoad);
        }
        #endregion
    }
}
