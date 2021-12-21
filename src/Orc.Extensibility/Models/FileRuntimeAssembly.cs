namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public class FileRuntimeAssembly : RuntimeAssembly
    {
        public FileRuntimeAssembly(string name, string source, string checksum, string location)
            : base(name, source, checksum)
        {
            Location = location;
        }

        public string Location { get; private set; }

        public override Stream GetStream()
        {
            return File.OpenRead(Location);
        }

        public override string ToString()
        {
            return $"{Name} ({Location})";
        }
    }
}
