namespace Orc.Extensibility;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMultiplePluginsService : IPluginService
{
    Task<IEnumerable<IPlugin>> ConfigureAndLoadPluginsAsync(params string[] requestedPlugins);
}