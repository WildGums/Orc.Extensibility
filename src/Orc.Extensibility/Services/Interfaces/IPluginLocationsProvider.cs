namespace Orc.Extensibility;

using System.Collections.Generic;

public interface IPluginLocationsProvider
{
    IReadOnlyList<string> GetPluginDirectories();
}
