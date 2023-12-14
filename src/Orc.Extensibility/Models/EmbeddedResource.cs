namespace Orc.Extensibility;

using System;

public unsafe class EmbeddedResource
{
    public EmbeddedResource(IRuntimeAssembly containerAssembly, string name, byte* start, int size)
    {
        ArgumentNullException.ThrowIfNull(containerAssembly);

        ContainerAssembly = containerAssembly;
        Name = name;
        Start = start;
        Size = size;
    }

    public IRuntimeAssembly ContainerAssembly { get; set; }

    public string Name { get; private set; }

    public byte* Start { get; private set; }

    public int Size { get; private set; }

    public override string ToString()
    {
        return $"{Name} (from {ContainerAssembly})";
    }
}