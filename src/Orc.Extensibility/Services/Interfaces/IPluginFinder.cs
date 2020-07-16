namespace Orc.Extensibility
{
    using System.Collections.Generic;

    public interface IPluginFinder
    {
        IEnumerable<IPluginInfo> FindPlugins();
    }
}
