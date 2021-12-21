namespace Orc.Extensibility
{
    using System.IO;
    using Catel;

    public class CosturaRuntimeAssembly : RuntimeAssembly
    {
        public CosturaRuntimeAssembly(EmbeddedResource embeddedResource)
        {
            EmbeddedResource = embeddedResource;
        }

        public CosturaRuntimeAssembly(string content)
        {
            var splitted = content.Split('|');

            ResourceName = splitted[0];
            Version = splitted[1];
            AssemblyName = splitted[2];
            RelativeFileName = splitted[3];
            Checksum = splitted[4];

            // Requires newer version of Costura
            if (splitted.Length > 5 &&
                long.TryParse(splitted[5], out var size))
            {
                Size = size;
            }

            Name = AssemblyName;
            Source = AssemblyName;
        }

        public string ResourceName { get; set; }

        public string Version { get; set; }

        public string AssemblyName { get; set; }

        public string RelativeFileName { get; set; }

        public long? Size { get; set; }

        public EmbeddedResource EmbeddedResource { get; set; }

        public override bool IsRuntime
        {
            get
            {
                var resourceName = ResourceName;
                if (!string.IsNullOrWhiteSpace(resourceName))
                {
                    return resourceName.ContainsIgnoreCase(".runtimes.");
                }

                return EmbeddedResource?.Name.ContainsIgnoreCase(".runtimes.") ?? false;
            }
        }

        public override Stream GetStream()
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{ResourceName}|{Version}|{AssemblyName}|{RelativeFileName}|{Checksum}|{Size}";
        }
    }
}
