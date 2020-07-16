namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Metadata;
    using Catel;
    using Catel.Reflection;

    public class PluginInfo : IPluginInfo
    {
        public PluginInfo(string location, Type type)
        {
            Argument.IsNotNull(() => location);
            Argument.IsNotNull(() => type);

            Location = location;
            Aliases = new List<string>();

            InitializeData(type);
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Version { get; set; }

        public string Company { get; set; }

        public string Customer { get; set; }

        public string Location { get; private set; }

        public string FullTypeName { get; private set; }

        public string AssemblyName { get; private set; }

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

        private void InitializeData(Type type)
        {
            FullTypeName = type.FullName;
            AssemblyName = type.Assembly.GetName().Name;

            Name = AssemblyName;
            Version = type.Assembly.Version();

            var customAttributes = type.Assembly.GetCustomAttributesData();

            Name = customAttributes.GetAttributeValue<AssemblyTitleAttribute>() as string ?? Name;
            Version = customAttributes.GetAttributeValue<AssemblyInformationalVersionAttribute>() as string ?? Version;
            Company = customAttributes.GetAttributeValue<AssemblyCompanyAttribute>() as string;
        }
    }
}
