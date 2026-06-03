namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Reflection;
using Catel.Reflection;

public class PluginInfo : IPluginInfo
{
    public PluginInfo(string location, Type pluginType)
        : this(location, pluginType, null)
    {
        // Leave empty
    }

    public PluginInfo(string location, Type pluginType, Type? pluginRegistrarType)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(pluginType);

        Plugin = new PluginTypeInfo(location, pluginType);

        if (pluginRegistrarType is not null)
        {
            PluginRegistrar = new PluginTypeInfo(location, pluginRegistrarType);
        }

        Location = location;
        Aliases = new List<string>();

        Name = Plugin.AssemblyName;
        Description = Name;
        Version = pluginType.Assembly.Version();

        var customAttributes = pluginType.Assembly.GetCustomAttributesData();

        Name = customAttributes.GetAttributeValue<AssemblyTitleAttribute>() as string ?? Name;
        Version = customAttributes.GetAttributeValue<AssemblyInformationalVersionAttribute>() as string ?? Version;
        Company = customAttributes.GetAttributeValue<AssemblyCompanyAttribute>() as string ?? string.Empty;
        Customer = string.Empty;
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public string Version { get; set; }

    public string Company { get; set; }

    public string Customer { get; set; }

    public string Location { get; private set; }

    public IPluginTypeInfo Plugin { get; init; }

    public IPluginTypeInfo? PluginRegistrar { get; init; }

    public object? Tag { get; set; }

    public List<string> Aliases { get; private set; }

    public override string ToString()
    {
        var value = !string.IsNullOrWhiteSpace(Customer) ? $"{Customer} - " : string.Empty;

        value += $"{Name} {Version}";

        if (!string.IsNullOrWhiteSpace(Company))
        {
            value += $", created by {Company}";
        }

        return value;
    }
}
