﻿namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.Reflection.Metadata;
    using Catel;

    public class PluginInfo : IPluginInfo
    {
        public PluginInfo(string location, MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            Argument.IsNotNull(() => location);
            Argument.IsNotNull(() => metadataReader);

            Location = location;
            Aliases = new List<string>();

            InitializeData(metadataReader, typeDefinition);
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Version { get; set; }

        public string Company { get; set; }

        public string Customer { get; set; }

        public string Location { get; private set; }

        public string FullTypeName { get; private set; }

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

        private void InitializeData(MetadataReader metadataReader, TypeDefinition typeDefinition)
        {
            Name = metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name);
            Version = metadataReader.GetAssemblyDefinition().Version.ToString(3);

            foreach (var customAttributeHandle in metadataReader.GetAssemblyDefinition().GetCustomAttributes())
            {
                var customAttribute = metadataReader.GetCustomAttribute(customAttributeHandle);

                //Name = customAssemblyAttributes.GetReflectionOnlyAttributeValue<AssemblyTitleAttribute>() as string ?? assembly.Title();
                //Version = customAssemblyAttributes.GetReflectionOnlyAttributeValue<AssemblyInformationalVersionAttribute>() as string ?? assembly.Version();
                //Company = customAssemblyAttributes.GetReflectionOnlyAttributeValue<AssemblyCompanyAttribute>() as string;

            }

            //Location = metadataReader.GetAssemblyDefinition()..Location;
            FullTypeName = metadataReader.GetString(typeDefinition.Name);
        }
    }
}
