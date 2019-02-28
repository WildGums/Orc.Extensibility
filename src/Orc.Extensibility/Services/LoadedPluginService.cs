// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedPluginService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Catel;
    using Catel.Logging;

    public class LoadedPluginService : ILoadedPluginService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, IPluginInfo> _loadedPlugins = new Dictionary<string, IPluginInfo>();

        public LoadedPluginService()
        {
            
        }
        
        public List<IPluginInfo> GetLoadedPlugins()
        {
            lock (_loadedPlugins)
            {
                return _loadedPlugins.Values.ToList();
            }
        }

        [ObsoleteEx(ReplacementTypeOrMember = "GetLoadedPlugins", TreatAsErrorFromVersion = "3.0", RemoveInVersion = "4.0")]
        public List<IPluginInfo> LoadedPlugins
        {
            get
            {
                return GetLoadedPlugins();
            }
        }

        public event EventHandler<PluginEventArgs> PluginLoaded;

        public void AddPlugin(IPluginInfo pluginInfo)
        {
            Argument.IsNotNull(() => pluginInfo);

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
}
