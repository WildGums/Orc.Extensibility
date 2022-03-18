namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.IO;
    using Catel;

    public class PluginLoadContext
    {
        public PluginLoadContext(RuntimeAssembly pluginRuntimeAssembly)
        {
            Argument.IsNotNull(() => pluginRuntimeAssembly);

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
