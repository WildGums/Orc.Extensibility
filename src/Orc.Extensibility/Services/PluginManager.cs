// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginManager.cs" company="WildGums">
//   Copyright (c) 2012 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Catel;

    public class PluginManager : IPluginManager
    {
        private readonly object _lock = new object();
        private readonly IPluginFinder _pluginFinder;
        private List<IPluginInfo> _plugins;

        public PluginManager(IPluginFinder pluginFinder)
        {
            Argument.IsNotNull(() => pluginFinder);

            _pluginFinder = pluginFinder;
        }

        public async Task<IEnumerable<IPluginInfo>> GetPluginsAsync(bool forceRefresh = false)
        {
            if (_plugins is null || forceRefresh)
            {
                await RefreshAsync();
            }

            lock (_lock)
            {
                return _plugins.ToArray();
            }
        }

        public async Task RefreshAsync()
        {
            var plugins = await _pluginFinder.FindPluginsAsync();

            lock (_lock)
            {
                _plugins = new List<IPluginInfo>(plugins.OrderBy(x => x.Name));
            }
        }
    }
}
