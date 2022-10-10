namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;

    public class PluginLoadContext
    {
        public PluginLoadContext(RuntimeAssembly pluginRuntimeAssembly)
        {
            ArgumentNullException.ThrowIfNull(pluginRuntimeAssembly);

            PluginRuntimeAssembly = pluginRuntimeAssembly;
            RuntimeAssemblies = new List<RuntimeAssembly>();
        }

        public RuntimeAssembly PluginRuntimeAssembly { get; }

        public List<RuntimeAssembly> RuntimeAssemblies { get; private set; }

        public override string ToString()
        {
            return $"{PluginRuntimeAssembly}";
        }
    }
}
