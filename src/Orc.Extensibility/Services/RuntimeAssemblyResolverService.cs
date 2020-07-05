namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.IO;
    using System.Linq;
    using System.Reflection.PortableExecutable;
    using System.Reflection;
    using Catel.Logging;
    using Orc.FileSystem;
    using Catel.Services;
    using System.Reflection.Metadata;
    using Catel;
    using MethodTimer;
    using Catel.Reflection;

    public class RuntimeAssemblyResolverService : IRuntimeAssemblyResolverService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;
        private readonly IAssemblyReflectionService _assemblyReflectionService;
        private readonly IAppDataService _appDataService;

        private readonly Dictionary<string, RuntimeAssembly> _extractedAssemblies = new Dictionary<string, RuntimeAssembly>(StringComparer.OrdinalIgnoreCase);

        public RuntimeAssemblyResolverService(IFileService fileService, IDirectoryService directoryService, 
            IAssemblyReflectionService assemblyReflectionService, IAppDataService appDataService)
        {
            _fileService = fileService;
            _directoryService = directoryService;
            _assemblyReflectionService = assemblyReflectionService;
            _appDataService = appDataService;

            TargetDirectory = DetermineTargetDirectory();

            _directoryService.Delete(TargetDirectory, true);
            _directoryService.Create(TargetDirectory);
        }

        public string TargetDirectory { get; private set; }

        public RuntimeAssembly[] GetRuntimeAssemblies()
        {
            return _extractedAssemblies.Values.ToArray();
        }

        public void RegisterAssembly(string assemblyLocation)
        {
            // TODO: We *could* consider lazy-loading, but let's pre-load for now
            UnpackCosturaEmbeddedAssemblies(assemblyLocation, TargetDirectory);
        }

        protected virtual string DetermineTargetDirectory()
        {
            return System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(Catel.IO.ApplicationDataTarget.UserLocal), "runtime");
        }

        [Time]
        protected virtual void UnpackCosturaEmbeddedAssemblies(string assemblyPath, string targetDirectory)
        {
            Log.Debug($"Unpacking all Costura embedded assemblies from '{assemblyPath}' to '{targetDirectory}'");

            using (var fileStream = _fileService.OpenRead(assemblyPath))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    if (!peReader.HasMetadata)
                    {
                        return;
                    }

                    var embeddedResources = FindEmbeddedResources(peReader, assemblyPath);

                    var costuraEmbeddedAssembliesFromMetadata = FindEmbeddedAssembliesViaMetadata(embeddedResources);
                    if (costuraEmbeddedAssembliesFromMetadata is null)
                    {
                        Log.Warning($"Files are embedded with an older version of Costura (< 5.x). It's recommended to update so metadata is embedded by Costura");

                        // Old version of Costura, just extract everything
                        foreach (var embeddedResource in embeddedResources)
                        {
                            if (!embeddedResource.Name.StartsWith("costura."))
                            {
                                continue;
                            }

                            // Dynamically create the info
                            var costuraEmbeddedAssembly = new CosturaEmbeddedAssembly(embeddedResource);

                            costuraEmbeddedAssembly.ResourceName = embeddedResource.Name;
                            costuraEmbeddedAssembly.RelativeFileName = embeddedResource.Name.Replace("costura.", string.Empty).Replace(".compressed", string.Empty);
                            costuraEmbeddedAssembly.AssemblyName = costuraEmbeddedAssembly.RelativeFileName.Replace(".dll", string.Empty).Replace(".exe", string.Empty);

                            ExtractAssemblyFromEmbeddedResource(costuraEmbeddedAssembly, targetDirectory);
                        }
                    }
                    else
                    {
                        // Extract only what is included (we know exactly what)
                        foreach (var costuraEmbeddedAssembly in costuraEmbeddedAssembliesFromMetadata)
                        {
                            ExtractAssemblyFromEmbeddedResource(costuraEmbeddedAssembly, targetDirectory);
                        }
                    }
                }
            }
        }

        protected List<EmbeddedResource> FindEmbeddedResources(PEReader peReader, string assemblyPath)
        {
            var embeddedResources = new List<EmbeddedResource>();

            var resourcesDirectory = peReader.PEHeaders.CorHeader.ResourcesDirectory;
            if (resourcesDirectory.Size <= 0)
            {
                return embeddedResources;
            }

            if (!peReader.PEHeaders.TryGetDirectoryOffset(resourcesDirectory, out var start))
            {
                Log.Warning($"could not obtain ResourcesDirectory offset");
                return embeddedResources;
            }

            var peImage = peReader.GetEntireImage();
            if (start + resourcesDirectory.Size >= peImage.Length)
            {
                Log.Warning($"Invalid resource offset {start} + length {resourcesDirectory.Size} greater than {peImage.Length}");
                return embeddedResources;
            }

            unsafe
            {
                var resourcesStart = peImage.Pointer + start;

                var mdReader = peReader.GetMetadataReader();

                foreach (var resourceHandle in mdReader.ManifestResources)
                {
                    var resource = mdReader.GetManifestResource(resourceHandle);

                    // Only care about embedded resources
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    var resourceName = mdReader.GetString(resource.Name);

                    //// Only care about costura
                    //if (!resourceName.StartsWithIgnoreCase("costura.") || resourceName.Contains(".pdb"))
                    //{
                    //    continue;
                    //}

                    if (resource.Offset < 0)
                    {
                        Log.Warning($"Unexpected offset {resource.Offset} for resource {resourceName}");
                        continue;
                    }

                    if (resource.Offset + sizeof(int) > resourcesDirectory.Size)
                    {
                        Log.Warning($"Offset {resource.Offset} leaves no room for size for resource {resourceName}");
                        continue;
                    }

                    var size = *(int*)(resourcesStart + resource.Offset);
                    if (size < 0)
                    {
                        Log.Warning($"Unexpected size {size} for resource {resourceName}");
                        continue;
                    }

                    if (resource.Offset + size > resourcesDirectory.Size)
                    {
                        Log.Warning($"Size {size} exceeds size of resource directory for resource {resourceName}");
                        continue;
                    }

                    var resourceStart = resourcesStart + resource.Offset + sizeof(int);

                    embeddedResources.Add(new EmbeddedResource
                    {
                        SourceAssemblyPath = assemblyPath,
                        Name = resourceName,
                        Start = resourceStart,
                        Size = size
                    });
                }
            }

            return embeddedResources;
        }

        protected List<CosturaEmbeddedAssembly> FindEmbeddedAssembliesViaMetadata(IEnumerable<EmbeddedResource> resources)
        {
            var metadataResource = (from x in resources
                                    where x.Name.EqualsIgnoreCase("costura.metadata")
                                    select x).FirstOrDefault();
            if (metadataResource is null)
            {
                // Not found, return null
                return null;
            }

            var embeddedResources = new List<CosturaEmbeddedAssembly>();

            unsafe
            {
                using (var resourceStream = new UnmanagedMemoryStream(metadataResource.Start, metadataResource.Size))
                {
                    var streamReader = new StreamReader(resourceStream);

                    while (streamReader.Peek() >= 0)
                    {
                        var line = streamReader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            var costuraEmbeddedResource = new CosturaEmbeddedAssembly(line);

                            var embeddedResource = (from x in resources
                                                    where x.Name == costuraEmbeddedResource.ResourceName
                                                    select x).FirstOrDefault();
                            if (embeddedResource is null)
                            {
                                Log.Error($"Expected to find Costura embedded resource '{costuraEmbeddedResource.ResourceName}', but could not find it");
                                continue;
                            }

                            costuraEmbeddedResource.EmbeddedResource = embeddedResource;

                            embeddedResources.Add(costuraEmbeddedResource);
                        }
                    }
                }
            }

            return embeddedResources;
        }

        protected virtual void ExtractAssemblyFromEmbeddedResource(CosturaEmbeddedAssembly costuraEmbeddedAssembly, string targetDirectory)
        {
            if (costuraEmbeddedAssembly.ResourceName.Contains(".pdb"))
            {
                // Ignore for now
                return;
            }

            if (_extractedAssemblies.ContainsKey(costuraEmbeddedAssembly.ResourceName))
            {
                // Already extracted
                return;
            }

            //var assemblyName = resourceName.Replace("costura.", string.Empty)
            //    .Replace(".dll.compressed", string.Empty);

            // TODO: Support resource names
            // TODO: Support pdb files

            //if (requestedAssemblyName.CultureInfo != null && !string.IsNullOrEmpty(requestedAssemblyName.CultureInfo.Name))
            //{
            //    text = requestedAssemblyName.CultureInfo.Name + "." + text;
            //}

            byte[] rawAssembly;

            unsafe
            {
                var embeddedResource = costuraEmbeddedAssembly.EmbeddedResource;

                using (var resourceStream = new UnmanagedMemoryStream(embeddedResource.Start, embeddedResource.Size))
                {
                    using (var assemblyStream = LoadStream(resourceStream, embeddedResource.Name))
                    {
                        if (assemblyStream is null)
                        {
                            return;
                        }

                        rawAssembly = ReadStream(assemblyStream);

                        // Note: in the future, we could use reflection to check the version and see if it's higher, if so, use that instead

                        var targetFileName = Path.Combine(targetDirectory, costuraEmbeddedAssembly.RelativeFileName);

                        var fileDirectory = Path.GetDirectoryName(targetFileName);

                        _directoryService.Create(fileDirectory);

                        using (var targetStream = _fileService.Create(targetFileName))
                        {
                            targetStream.Write(rawAssembly, 0, rawAssembly.Length);
                            targetStream.Flush();
                        }

                        var assemblyName = !string.IsNullOrWhiteSpace(costuraEmbeddedAssembly.AssemblyName) ?
                            TypeHelper.GetAssemblyNameWithoutOverhead(costuraEmbeddedAssembly.AssemblyName) :
                            costuraEmbeddedAssembly.ResourceName;

                        _extractedAssemblies.Add(costuraEmbeddedAssembly.ResourceName, 
                            new RuntimeAssembly(assemblyName, targetFileName, embeddedResource.SourceAssemblyPath));

                        // Could be nested, extract this one
                        RegisterAssembly(targetFileName);
                    }
                }
            }

            //using (var symbolStream = LoadStream(symbolNames, text))
            //{
            //    if (symbolStream is null == false)
            //    {
            //        byte[] rawSymbolStore = ReadStream(symbolStream);
            //        return Assembly.Load(rawAssembly, rawSymbolStore);
            //    }
            //}
        }

        private Stream LoadStream(Stream existingStream, string resourceName)
        {
            if (resourceName.EndsWith(".compressed"))
            {
                using (var source = new DeflateStream(existingStream, CompressionMode.Decompress))
                {
                    var memoryStream = new MemoryStream();
                    CopyTo(source, memoryStream);
                    memoryStream.Position = 0L;
                    return memoryStream;
                }
            }

            return existingStream;
        }

        private void CopyTo(Stream source, Stream destination)
        {
            byte[] array = new byte[81920];
            int count;
            while ((count = source.Read(array, 0, array.Length)) != 0)
            {
                destination.Write(array, 0, count);
            }
        }

        private byte[] ReadStream(Stream stream)
        {
            byte[] array = new byte[stream.Length];
            stream.Read(array, 0, array.Length);
            return array;
        }

        public unsafe class EmbeddedResource
        {
            public string SourceAssemblyPath { get; set; }

            public string Name { get; set; }

            public byte* Start { get; set; }

            public int Size { get; set; }
        }

        public class CosturaEmbeddedAssembly
        {
            public CosturaEmbeddedAssembly(EmbeddedResource embeddedResource)
            {
                EmbeddedResource = embeddedResource;
            }

            public CosturaEmbeddedAssembly(string content)
            {
                var splitted = content.Split('|');

                ResourceName = splitted[0];
                Version = splitted[1];
                AssemblyName = splitted[2];
                RelativeFileName = splitted[3];
                Checksum = splitted[4];
            }

            public string ResourceName { get; set; }

            public string Version { get; set; }

            public string AssemblyName { get; set; }

            public string RelativeFileName { get; set; }

            public string Checksum { get; set; }
            public EmbeddedResource EmbeddedResource { get; set; }

            public override string ToString()
            {
                return $"{ResourceName}|{Version}|{AssemblyName}|{RelativeFileName}|{Checksum}";
            }
        }
    }
}
