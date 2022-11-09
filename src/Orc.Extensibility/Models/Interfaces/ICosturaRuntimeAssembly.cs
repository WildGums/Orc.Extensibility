namespace Orc.Extensibility
{
    public interface ICosturaRuntimeAssembly : IRuntimeAssembly
    {
        EmbeddedResource? EmbeddedResource { get; set; }
        string RelativeFileName { get; set; }
        string ResourceName { get; set; }
    }
}
