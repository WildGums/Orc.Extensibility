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
    using System.Security.Cryptography;
    using System.Text;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public class RuntimeAssemblyResolverService : IRuntimeAssemblyResolverService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;
        private readonly IAssemblyReflectionService _assemblyReflectionService;
        private readonly IAppDataService _appDataService;

        private readonly Dictionary<string, PluginLoadContext> _pluginLoadContexts = new Dictionary<string, PluginLoadContext>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _processedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public RuntimeAssemblyResolverService(IFileService fileService, IDirectoryService directoryService,
            IAssemblyReflectionService assemblyReflectionService, IAppDataService appDataService)
        {
            _fileService = fileService;
            _directoryService = directoryService;
            _assemblyReflectionService = assemblyReflectionService;
            _appDataService = appDataService;

            TargetDirectory = DetermineTargetDirectory();

            // Note: deleting might fail when files are in use, that should be possible, 
            // we will only overwrite
            _directoryService.Create(TargetDirectory);
        }

        public string TargetDirectory { get; private set; }

        public PluginLoadContext[] GetPluginLoadContexts()
        {
            return _pluginLoadContexts.Values.ToArray();
        }

        public async Task RegisterAssemblyAsync(string assemblyLocation)
        {
            var fileName = Path.GetFileNameWithoutExtension(assemblyLocation);
            var targetDirectory = Path.Combine(TargetDirectory, fileName);

            if (_pluginLoadContexts.ContainsKey(assemblyLocation))
            {
                return;
            }

            var pluginLoadContext = new PluginLoadContext(assemblyLocation, targetDirectory);
            _pluginLoadContexts[assemblyLocation] = pluginLoadContext;

            await RegisterAssemblyAsync(pluginLoadContext, null, assemblyLocation);
        }

        protected async Task RegisterAssemblyAsync(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, string assemblyLocation)
        {
            // TODO: We *could* consider lazy-loading, but let's pre-load for now
            await UnpackCosturaEmbeddedAssembliesAsync(pluginLoadContext, originatingAssembly, assemblyLocation);
        }

        protected virtual string DetermineTargetDirectory()
        {
            return System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(Catel.IO.ApplicationDataTarget.UserLocal), "runtime");
        }

        [Time("{assemblyPath}")]
        protected virtual async Task UnpackCosturaEmbeddedAssembliesAsync(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, string assemblyPath)
        {
            // Ignore specific assemblies
            if (ShouldIgnoreAssemblyForCosturaExtracting(pluginLoadContext, originatingAssembly, assemblyPath))
            {
                return;
            }

            Log.Debug($"Unpacking all Costura embedded assemblies from '{assemblyPath}' to '{pluginLoadContext.RuntimeDirectory}'");

            using (var fileStream = _fileService.OpenRead(assemblyPath))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    if (!peReader.HasMetadata)
                    {
                        return;
                    }

                    var embeddedResources = await FindEmbeddedResourcesAsync(peReader, assemblyPath);
                    if (embeddedResources.Count == 0)
                    {
                        // Not using Costura
                        return;
                    }

                    var costuraEmbeddedAssembliesFromMetadata = await FindEmbeddedAssembliesViaMetadataAsync(embeddedResources);
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

                            await ExtractAssemblyFromEmbeddedResourceAsync(pluginLoadContext, originatingAssembly, costuraEmbeddedAssembly);
                        }
                    }
                    else
                    {
                        // Extract only what is included (we know exactly what)
                        foreach (var costuraEmbeddedAssembly in costuraEmbeddedAssembliesFromMetadata)
                        {
                            await ExtractAssemblyFromEmbeddedResourceAsync(pluginLoadContext, originatingAssembly, costuraEmbeddedAssembly);
                        }
                    }
                }
            }
        }

        protected virtual bool ShouldIgnoreAssemblyForCosturaExtracting(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, string assemblyPath)
        {
            if (assemblyPath.ContainsIgnoreCase(".resources.dll"))
            {
                return true;
            }

            if (_processedAssemblies.Contains(assemblyPath))
            {
                return true;
            }

            return false;
        }

        protected async Task<List<EmbeddedResource>> FindEmbeddedResourcesAsync(PEReader peReader, string assemblyPath)
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

                    // Only care about costura for now
                    if (!resourceName.StartsWithIgnoreCase("costura"))
                    {
                        continue;
                    }

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

        protected async Task<List<CosturaEmbeddedAssembly>> FindEmbeddedAssembliesViaMetadataAsync(IEnumerable<EmbeddedResource> resources)
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
#pragma warning disable CL0001 // Use async overload inside this async method
                        var line = streamReader.ReadLine();
#pragma warning restore CL0001 // Use async overload inside this async method
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

        [Time]
        protected virtual async Task ExtractAssemblyFromEmbeddedResourceAsync(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, CosturaEmbeddedAssembly costuraEmbeddedAssembly)
        {
            // TODO: Support resource names
            // TODO: Support pdb files

            //if (requestedAssemblyName.CultureInfo != null && !string.IsNullOrEmpty(requestedAssemblyName.CultureInfo.Name))
            //{
            //    text = requestedAssemblyName.CultureInfo.Name + "." + text;
            //}

            if (costuraEmbeddedAssembly.ResourceName.Contains(".pdb"))
            {
                // Ignore for now
                return;
            }

            if (pluginLoadContext.RuntimeAssemblies.Any(x => x.Location.EndsWith(costuraEmbeddedAssembly.RelativeFileName)))
            {
                // Already extracted
                return;
            }

#if NETCORE
            if (costuraEmbeddedAssembly.IsRuntime)
            {
                if (!PlatformInformation.RuntimeIdentifiers.Any(x => costuraEmbeddedAssembly.RelativeFileName.ContainsIgnoreCase($"/{x}/")))
                {
                    Log.Debug($"Ignoring '{costuraEmbeddedAssembly}' since it's not applicable to the current platform");

                    // Not for this platform
                    return;
                }
            }
#endif

            var extract = true;
            var targetDirectory = pluginLoadContext.RuntimeDirectory;
            var targetFileName = Path.Combine(targetDirectory, costuraEmbeddedAssembly.RelativeFileName);

            if (_fileService.Exists(targetFileName))
            {
                var clearFile = false;

                // Step 1: check size
                if (costuraEmbeddedAssembly.Size.HasValue &&
                    new FileInfo(targetFileName).Length != costuraEmbeddedAssembly.Size)
                {
                    Log.Debug($"File '{targetFileName}' has incorrect size, deleting file and re-extracting...");

                    clearFile = true;
                }

                if (!clearFile)
                {
                    // Step 2: check sha1 hash
                    var checksum = string.Empty;

                    using (var existingFileStream = _fileService.OpenRead(targetFileName))
                    {
                        checksum = await CalculateSha1ChecksumAsync(existingFileStream);
                    }

                    if (!checksum.EqualsIgnoreCase(costuraEmbeddedAssembly.Sha1Checksum))
                    {
                        Log.Debug($"File '{targetFileName}' has incorrect size, deleting file and re-extracting...");

                        clearFile = true;
                    }
                }

                if (clearFile)
                {
                    _fileService.Delete(targetFileName);
                }
                else
                {
                    Log.Debug($"File '{targetFileName}' has correct size and hash, no need for re-extracting");
                }

                extract = clearFile;
            }

            var embeddedResource = costuraEmbeddedAssembly.EmbeddedResource;

            if (extract)
            {
                await ExtractAssemblyFromEmbeddedResourceUncachedAsync(pluginLoadContext, originatingAssembly, costuraEmbeddedAssembly, targetFileName);
            }

            var assemblyName = !string.IsNullOrWhiteSpace(costuraEmbeddedAssembly.AssemblyName) ?
                TypeHelper.GetAssemblyNameWithoutOverhead(costuraEmbeddedAssembly.AssemblyName) :
                costuraEmbeddedAssembly.ResourceName;

            var runtimeAssembly = new RuntimeAssembly(assemblyName, targetFileName, embeddedResource.SourceAssemblyPath)
            {
                IsRuntime = costuraEmbeddedAssembly.ResourceName.ContainsIgnoreCase(".runtimes.")
            };

            originatingAssembly?.Dependencies.Add(runtimeAssembly);

            pluginLoadContext.RuntimeAssemblies.Add(runtimeAssembly);

            //using (var symbolStream = LoadStream(symbolNames, text))
            //{
            //    if (symbolStream is not null)
            //    {
            //        byte[] rawSymbolStore = ReadStream(symbolStream);
            //        return Assembly.Load(rawAssembly, rawSymbolStore);
            //    }
            //}

            // Could be nested, extract this one
            await RegisterAssemblyAsync(pluginLoadContext, runtimeAssembly, targetFileName);
        }

        [Time]
        protected virtual async Task ExtractAssemblyFromEmbeddedResourceUncachedAsync(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, CosturaEmbeddedAssembly costuraEmbeddedAssembly, string targetFileName)
        {
            Log.Debug($"Extracting embedded assembly '{costuraEmbeddedAssembly.ResourceName}' to '{targetFileName}'");

            var embeddedResource = costuraEmbeddedAssembly.EmbeddedResource;

            Stream resourceStream = null;

            unsafe
            {
                resourceStream = new UnmanagedMemoryStream(embeddedResource.Start, embeddedResource.Size);
            }

            try
            {
                using (var assemblyStream = await LoadStreamAsync(resourceStream, embeddedResource.Name))
                {
                    if (assemblyStream is null)
                    {
                        return;
                    }

                    var rawAssembly = await ReadStreamAsync(assemblyStream);

                    var fileDirectory = Path.GetDirectoryName(targetFileName);

                    _directoryService.Create(fileDirectory);

                    using (var targetStream = _fileService.Create(targetFileName))
                    {
                        await targetStream.WriteAsync(rawAssembly, 0, rawAssembly.Length);
                        await targetStream.FlushAsync();
                    }
                }
            }
            finally
            {
                resourceStream?.Dispose();
            }
        }

        [Time]
        protected virtual async Task<string> CalculateSha1ChecksumAsync(Stream stream)
        {
            // Performance idea: do we want to cache hashes for already extracted assemblies?

            using (var bs = new BufferedStream(stream))
            {
                using (var sha1 = new SHA1CryptoServiceProvider())
                {
#if NETCOREAPP3_1
                    var hash = sha1.ComputeHash(bs);
#else
                    // Note: don't use ComputeHasAsync, is very slow!
                    //var hash = await sha1.ComputeHashAsync(bs);
                    var hash = sha1.ComputeHash(bs);
#endif
                    var formatted = new StringBuilder(2 * hash.Length);

                    foreach (var b in hash)
                    {
                        formatted.AppendFormat("{0:X2}", b);
                    }

                    return formatted.ToString();
                }
            }
        }

        private async Task<Stream> LoadStreamAsync(Stream existingStream, string resourceName)
        {
            if (resourceName.EndsWith(".compressed"))
            {
                using (var source = new DeflateStream(existingStream, CompressionMode.Decompress))
                {
                    var memoryStream = new MemoryStream();

                    await CopyToAsync(source, memoryStream);

                    memoryStream.Position = 0L;
                    return memoryStream;
                }
            }

            return existingStream;
        }

        private async Task CopyToAsync(Stream source, Stream destination)
        {
            var array = new byte[81920];
            int count;

            while ((count = source.Read(array, 0, array.Length)) != 0)
            {
                await destination.WriteAsync(array, 0, count);
            }

            await destination.FlushAsync();
        }

        private async Task<byte[]> ReadStreamAsync(Stream stream)
        {
            var array = new byte[stream.Length];
            await stream.ReadAsync(array, 0, array.Length);
            return array;
        }

        public unsafe class EmbeddedResource
        {
            public string SourceAssemblyPath { get; set; }

            public string Name { get; set; }

            public byte* Start { get; set; }

            public int Size { get; set; }

            public override string ToString()
            {
                return $"{Name} ({SourceAssemblyPath})";
            }
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
                Sha1Checksum = splitted[4];

                // Requires newer version of Costura
                if (splitted.Length > 5 &&
                    long.TryParse(splitted[5], out var size))
                {
                    Size = size;
                }
            }

            public string ResourceName { get; set; }

            public string Version { get; set; }

            public string AssemblyName { get; set; }

            public string RelativeFileName { get; set; }

            public string Sha1Checksum { get; set; }

            public long? Size { get; set; }

            public bool IsRuntime
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

            public EmbeddedResource EmbeddedResource { get; set; }

            public override string ToString()
            {
                return $"{ResourceName}|{Version}|{AssemblyName}|{RelativeFileName}|{Sha1Checksum}|{Size}";
            }
        }
    }
}
