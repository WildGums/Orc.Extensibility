// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginManager.cs" company="WildGums">
//   Copyright (c) 2012 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.Linq;
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

        public IEnumerable<IPluginInfo> GetPlugins(bool forceRefresh = false)
        {
            lock (_lock)
            {
                if (_plugins is null || forceRefresh)
                {
                    Refresh();
                }

                return _plugins.ToArray();
            }
        }

        public void Refresh()
        {
            lock (_lock)
            {
                _plugins = new List<IPluginInfo>(_pluginFinder.FindPlugins().OrderBy(x => x.Name));
            }
        }
    }
}
