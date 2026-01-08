namespace Orc.Extensibility;

public interface IPluginFactory
{
    object CreatePluginType(IPluginTypeInfo pluginTypeInfo);
}
