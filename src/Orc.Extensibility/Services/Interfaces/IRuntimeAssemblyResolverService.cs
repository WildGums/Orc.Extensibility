namespace Orc.Extensibility;

using System.Threading.Tasks;

public interface IRuntimeAssemblyResolverService
{
    IPluginLoadContext[] GetPluginLoadContexts();
    Task RegisterAssemblyAsync(IRuntimeAssembly runtimeAssembly);
    Task UnregisterAssemblyAsync(IRuntimeAssembly runtimeAssembly);
}