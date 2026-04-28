namespace Orc.Extensibility;

using System;
using Catel.Reflection;

public class PluginTypeInfo : IPluginTypeInfo
{
    public PluginTypeInfo(string location, Type type)
    {
        Location = location;
        FullTypeName = type.GetSafeFullName();
        AssemblyName = type.Assembly.GetName().Name ?? string.Empty;
    }

    public string Location { get; private set; }

    public string FullTypeName { get; private set; }

    public string AssemblyName { get; private set; }

    public override string ToString()
    {
        return FullTypeName;
    }
}
