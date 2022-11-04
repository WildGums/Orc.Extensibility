namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.IO;

    public abstract class RuntimeAssembly
    {
        protected RuntimeAssembly()
        {
            Dependencies = new List<RuntimeAssembly>();
            Name = string.Empty;
            Source = string.Empty;
            Checksum = string.Empty;
        }

        protected RuntimeAssembly(string name, string source, string checksum)
            : this()
        {
            Name = name;
            Source = source;
            Checksum = checksum;
        }

        public string Name { get; protected set; }

        public string Source { get; protected set; }

        public virtual bool IsRuntime { get; protected set; }

        public bool IsLoaded { get; protected set; }

        public string Checksum { get; set; }

        public List<RuntimeAssembly> Dependencies { get; private set; }

        public abstract Stream GetStream();

        public void MarkLoaded()
        {
            IsLoaded = true;

            OnMarkLoaded();
        }

        protected abstract void OnMarkLoaded();

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
