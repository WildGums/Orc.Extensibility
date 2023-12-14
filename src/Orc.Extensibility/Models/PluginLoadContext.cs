namespace Orc.Extensibility;

using System;
using System.Collections.Generic;

public class PluginLoadContext : IPluginLoadContext
{
    public PluginLoadContext(IRuntimeAssembly pluginRuntimeAssembly)
    {
        ArgumentNullException.ThrowIfNull(pluginRuntimeAssembly);

        PluginRuntimeAssembly = pluginRuntimeAssembly;
        RuntimeAssemblies = new List<IRuntimeAssembly>();
    }

    public IRuntimeAssembly PluginRuntimeAssembly { get; }

    public List<IRuntimeAssembly> RuntimeAssemblies { get; private set; }

    public override string ToString()
    {
        return $"{PluginRuntimeAssembly}";
    }
}