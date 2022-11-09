namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using Catel;

    public class PluginLoadContext : IPluginLoadContext
    {
        public PluginLoadContext(IRuntimeAssembly pluginRuntimeAssembly)
        {
            Argument.IsNotNull(() => pluginRuntimeAssembly);

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
}
