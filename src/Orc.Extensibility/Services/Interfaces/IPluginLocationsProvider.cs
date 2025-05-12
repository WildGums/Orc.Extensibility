namespace Orc.Extensibility;

using System.Collections.Generic;

public interface IPluginLocationsProvider
{
    [ObsoleteEx(ReplacementTypeOrMember = nameof(GetPluginLocations), TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    IReadOnlyList<string> GetPluginDirectories();

    IReadOnlyList<PluginProbingLocation> GetPluginLocations();
}
