namespace Orc.Extensibility
{
    public unsafe class EmbeddedResource
    {
        public RuntimeAssembly ContainerAssembly { get; set; }

        public string Name { get; set; }

        public byte* Start { get; set; }

        public int Size { get; set; }

        public override string ToString()
        {
            return $"{Name} (from {ContainerAssembly})";
        }
    }
}
