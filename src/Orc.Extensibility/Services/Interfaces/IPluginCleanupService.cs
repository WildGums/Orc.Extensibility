namespace Orc.Extensibility;

public interface IPluginCleanupService
{
    bool IsCleanupRequired(string directory);
    void Cleanup(string directory);
}