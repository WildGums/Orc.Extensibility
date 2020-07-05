namespace Orc.Extensibility
{
    public interface IRuntimeAssemblyResolverService
    {
        string TargetDirectory { get; }
        PluginLoadContext[] GetPluginLoadContexts();
        void RegisterAssembly(string assemblyLocation);
    }
}
