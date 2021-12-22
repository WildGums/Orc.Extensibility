namespace Orc.Extensibility
{
    using System.Threading.Tasks;

    public interface IRuntimeAssemblyResolverService
    {
        PluginLoadContext[] GetPluginLoadContexts();
        Task RegisterAssemblyAsync(RuntimeAssembly runtimeAssembly);
        Task UnregisterAssemblyAsync(RuntimeAssembly runtimeAssembly);
    }
}
