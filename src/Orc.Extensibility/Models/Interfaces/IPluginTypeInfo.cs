namespace Orc.Extensibility
{
    public interface IPluginTypeInfo
    {
        string Location { get; }
        string FullTypeName { get; }
        string AssemblyName { get; }
    }
}
