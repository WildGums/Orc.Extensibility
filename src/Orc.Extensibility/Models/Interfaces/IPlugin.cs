namespace Orc.Extensibility
{
    public interface IPlugin
    {
        object Instance { get; }
        IPluginInfo Info { get; }
    }
}
