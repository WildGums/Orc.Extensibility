namespace Orc.Extensibility
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Catel;

    public class MemoryRuntimeAssembly : RuntimeAssembly
    {
        public MemoryRuntimeAssembly(string name, string source, string checksum, Assembly containerAssembly, string resourceName)
            : base(name, source, checksum)
        {
            Argument.IsNotNull(() => containerAssembly);

            ContainerAssembly = containerAssembly;
            ResourceName = resourceName;
        }

        public Assembly ContainerAssembly { get; private set; }

        public string ResourceName { get; private set; }

        public override Stream GetStream()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
