namespace Orc.Extensibility;

public interface IPluginFactory
{
    object CreatePlugin(IPluginInfo pluginInfo);
}