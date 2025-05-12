namespace Orc.Extensibility;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMultiplePluginsService : IPluginService
{
    Task<IReadOnlyList<IPlugin>> ConfigureAndLoadPluginsAsync(params string[] requestedPlugins);
}
