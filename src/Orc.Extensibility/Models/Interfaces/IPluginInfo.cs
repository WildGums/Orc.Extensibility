namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;

    public interface IPluginInfo
    {
        string Name { get; set; }
        string Description { get; set; }
        string Version { get; set; }
        string Company { get; set; }
        string Customer { get; set; }

        string Location { get; }
        string FullTypeName { get; }
        string AssemblyName { get; }

        List<string> Aliases { get; }
    }
}
