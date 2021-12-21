namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public abstract class RuntimeAssembly
    {
        protected RuntimeAssembly(string name, string source, string checksum)
        {
            Name = name;
            Source = source;
            Checksum = checksum;
            Dependencies = new List<RuntimeAssembly>();
        }

        public string Name { get; private set; }

        public string Source { get; private set; }

        public bool IsRuntime { get; set; }

        public string Checksum { get; set; }

        public List<RuntimeAssembly> Dependencies { get; private set; }

        public abstract Stream GetStream();

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
