namespace Orc.Extensibility;

using System.Collections.Generic;

public interface IPluginInfo
{
    string Name { get; set; }
    string Description { get; set; }
    string Version { get; set; }
    string Company { get; set; }
    string Customer { get; set; }

    string Location { get; }

    IPluginTypeInfo Plugin { get; init; }

    IPluginTypeInfo? PluginRegistrar { get; init; }

    object? Tag { get; }

    List<string> Aliases { get; }
}
