namespace Orc.Extensibility
{
    using System.Threading.Tasks;

    public interface IRuntimeAssemblyResolverService
    {
        string TargetDirectory { get; }
        PluginLoadContext[] GetPluginLoadContexts();
        Task RegisterAssemblyAsync(string assemblyLocation);
        Task UnregisterAssemblyAsync(string assemblyLocation);
    }
}
