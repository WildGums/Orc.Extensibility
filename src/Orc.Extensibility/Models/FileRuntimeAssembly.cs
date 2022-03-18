namespace Orc.Extensibility
{
    using System.IO;

    public class FileRuntimeAssembly : RuntimeAssembly
    {
        public FileRuntimeAssembly(string location)
            : this(location, location, location, location)
        {
            // Keep empty by design
        }

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

        public override void MarkLoaded()
        {
            // no implementation needed
        }

        public override string ToString()
        {
            return $"{Name} ({Location})";
        }
    }
}
