namespace Orc.Extensibility;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRuntimeAssemblyResolverService
{
    IReadOnlyList<IPluginLoadContext> GetPluginLoadContexts();
    Task RegisterAssemblyAsync(IRuntimeAssembly runtimeAssembly);
    Task UnregisterAssemblyAsync(IRuntimeAssembly runtimeAssembly);
}
