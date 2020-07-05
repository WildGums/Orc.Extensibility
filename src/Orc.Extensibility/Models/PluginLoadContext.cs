namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.IO;

    public class PluginLoadContext
    {
        public PluginLoadContext(string pluginLocation, string runtimeDirectory)
        {
            PluginLocation = pluginLocation;
            RuntimeDirectory = runtimeDirectory;
            DependenciesFilePath = Path.ChangeExtension(pluginLocation, "deps.json");

            RuntimeAssemblies = new List<RuntimeAssembly>();
        }

        public string PluginLocation { get; }
        public string RuntimeDirectory { get; }
        public string DependenciesFilePath { get; }

        public List<RuntimeAssembly> RuntimeAssemblies { get; private set; }
    }
}
