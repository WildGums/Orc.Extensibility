namespace Orc.Extensibility;

using System.Collections.Generic;

public interface IPluginLoadContext
{
    IRuntimeAssembly PluginRuntimeAssembly { get; }
    List<IRuntimeAssembly> RuntimeAssemblies { get; }
}