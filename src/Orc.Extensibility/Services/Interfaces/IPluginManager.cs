namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPluginManager
    {
        IEnumerable<IPluginInfo> GetPlugins();
        Task RefreshAsync();
    }
}
