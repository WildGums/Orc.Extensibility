namespace Orc.Extensibility
{
    using System.Collections.Generic;

    public class RuntimeAssembly
    {
        public RuntimeAssembly(string name, string location, string source)
        {
            Name = name;
            Location = location;
            Source = source;
            Dependencies = new List<RuntimeAssembly>();
        }

        public string Name { get; private set; }

        public string Location { get; private set; }

        public string Source { get; private set; }

        public bool IsRuntime { get; set; }

        public List<RuntimeAssembly> Dependencies { get; private set; }
    }
}
