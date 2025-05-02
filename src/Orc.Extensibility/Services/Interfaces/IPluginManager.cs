namespace Orc.Extensibility;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPluginManager
{
    IReadOnlyList<IPluginInfo> GetPlugins();
    Task RefreshAsync();
}
