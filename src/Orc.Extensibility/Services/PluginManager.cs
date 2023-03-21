namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Catel.Logging;

public class PluginManager : IPluginManager
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private readonly object _lock = new object();
    private readonly IPluginFinder _pluginFinder;

    private List<IPluginInfo>? _plugins;

    public PluginManager(IPluginFinder pluginFinder)
    {
        ArgumentNullException.ThrowIfNull(pluginFinder);

        _pluginFinder = pluginFinder;
    }

    public IEnumerable<IPluginInfo> GetPlugins()
    {
        lock (_lock)
        {
            if (_plugins is null)
            {
                throw Log.ErrorAndCreateException<InvalidOperationException>("Make sure to call RefreshAsync method at least once before using this method");
            }

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