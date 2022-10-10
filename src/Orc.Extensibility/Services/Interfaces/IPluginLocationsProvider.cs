namespace Orc.Extensibility
{
    using System.Collections.Generic;

    public interface IPluginLocationsProvider
    {
        IEnumerable<string> GetPluginDirectories();
    }
}
