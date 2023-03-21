namespace Orc.Extensibility;

using System.Collections.Generic;
using System.IO;

public interface IRuntimeAssembly
{
    string Checksum { get; set; }
    List<IRuntimeAssembly> Dependencies { get; }
    bool IsLoaded { get; }
    bool IsRuntime { get; }
    string Name { get; }
    string Source { get; }

    Stream GetStream();
    void MarkLoaded();
}