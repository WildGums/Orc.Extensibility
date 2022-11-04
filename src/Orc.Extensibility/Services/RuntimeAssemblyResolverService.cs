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

    public partial class RuntimeAssemblyResolverService : IRuntimeAssemblyResolverService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly string DirectorySeparator = Path.DirectorySeparatorChar.ToString();

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
        }

        public PluginLoadContext[] GetPluginLoadContexts()
        {
            return _pluginLoadContexts.Values.ToArray();
        }

        public async Task RegisterAssemblyAsync(RuntimeAssembly runtimeAssembly)
        {
            var fileNameWithExtension = Path.GetFileName(runtimeAssembly.Source);

            var refAssemblyPath = $"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}{fileNameWithExtension}";
            if (runtimeAssembly.Source.EndsWithIgnoreCase(refAssemblyPath))
            {
                // Ignore ref assemblies
                return;
            }

            if (_pluginLoadContexts.ContainsKey(runtimeAssembly.Checksum))
            {
                return;
            }

            Log.Debug($"Registering runtime assembly resolving for '{runtimeAssembly}'");

            var pluginLoadContext = new PluginLoadContext(runtimeAssembly);
            _pluginLoadContexts[runtimeAssembly.Checksum] = pluginLoadContext;

            await RegisterAssemblyAsync(pluginLoadContext, null, runtimeAssembly);
        }

        public async Task UnregisterAssemblyAsync(RuntimeAssembly runtimeAssembly)
        {
            if (runtimeAssembly is null)
            {
                return;
            }

            if (_pluginLoadContexts.Remove(runtimeAssembly.Checksum))
            {
                Log.Debug("Unregistered runtime assembly");
            }
        }

        protected async Task RegisterAssemblyAsync(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, RuntimeAssembly runtimeAssembly)
        {
            var assemblies = await IndexCosturaEmbeddedAssembliesAsync(pluginLoadContext, originatingAssembly, runtimeAssembly);
            pluginLoadContext.RuntimeAssemblies.AddRange(assemblies);
        }

        [Time("{runtimeAssembly}")]
        protected virtual async Task<IEnumerable<RuntimeAssembly>> IndexCosturaEmbeddedAssembliesAsync(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, RuntimeAssembly runtimeAssembly)
        {
            // Ignore specific assemblies
            if (ShouldIgnoreAssemblyForCosturaExtracting(pluginLoadContext, originatingAssembly, runtimeAssembly))
            {
                return Array.Empty<RuntimeAssembly>();
            }

            Log.Debug($"Indexing all Costura embedded assemblies from '{runtimeAssembly}'");

            var indexedCosturaAssemblies = new List<RuntimeAssembly>();

            try
            {
                if (runtimeAssembly.IsLoaded)
                {
                    return indexedCosturaAssemblies;
                }

                using (var runtimeAssemblyStream = runtimeAssembly.GetStream())
                {
                    using (var peReader = new PEReader(runtimeAssemblyStream))
                    {
                        if (peReader.HasMetadata)
                        {
                            var embeddedResources = await FindEmbeddedResourcesAsync(peReader, runtimeAssembly);
                            if (embeddedResources.Count > 0)
                            {
                                var costuraEmbeddedAssembliesFromMetadata = await FindEmbeddedAssembliesViaMetadataAsync(embeddedResources);
                                if (costuraEmbeddedAssembliesFromMetadata is null)
                                {
                                    Log.Error($"Files are embedded with an older version of Costura (< 5.x). It's required to update so metadata is embedded by Costura");
                                    return indexedCosturaAssemblies;
                                }

                                // Extract only what is included (we know exactly what)
                                foreach (var costuraEmbeddedAssembly in costuraEmbeddedAssembliesFromMetadata)
                                {
                                    if (costuraEmbeddedAssembly.IsRuntime)
                                    {
                                        // Check if correct platform
                                        if (!PlatformInformation.RuntimeIdentifiers.Any(x => costuraEmbeddedAssembly.RelativeFileName.ContainsIgnoreCase($"/{x}/")))
                                        {
                                            Log.Debug($"Ignoring '{costuraEmbeddedAssembly}' since it's not applicable to the current platform");

                                            // Not for this platform
                                            continue;
                                        }

                                        // Preload stream for assemblies that are not being checked
                                        costuraEmbeddedAssembly.PreloadStream();

                                        // Runtimes never contain embedded assemblies, so only add
                                        indexedCosturaAssemblies.Add(costuraEmbeddedAssembly);
                                        continue;
                                    }

                                    if (costuraEmbeddedAssembly.RelativeFileName.EndsWithIgnoreCase(".resources.dll"))
                                    {
                                        // Preload stream for assemblies that are not being checked
                                        costuraEmbeddedAssembly.PreloadStream();

                                        // Resources never contain embedded assemblies, so only add
                                        indexedCosturaAssemblies.Add(costuraEmbeddedAssembly);
                                        continue;
                                    }

                                    if (costuraEmbeddedAssembly.RelativeFileName.EndsWithIgnoreCase(".pdb"))
                                    {
                                        // Ignore pdb files
                                        continue;
                                    }

                                    indexedCosturaAssemblies.Add(costuraEmbeddedAssembly);

                                    // Recursive indexing
                                    var recursiveRuntimeAssemblies = await IndexCosturaEmbeddedAssembliesAsync(pluginLoadContext, originatingAssembly, costuraEmbeddedAssembly);
                                    if (recursiveRuntimeAssemblies.Any())
                                    {
                                        indexedCosturaAssemblies.AddRange(recursiveRuntimeAssemblies);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"Failed to index Costura assemblies for '{runtimeAssembly}'");
            }

            return indexedCosturaAssemblies;
        }

        protected virtual bool ShouldIgnoreAssemblyForCosturaExtracting(PluginLoadContext pluginLoadContext, RuntimeAssembly originatingAssembly, RuntimeAssembly runtimeAssembly)
        {
            if (runtimeAssembly.Name.ContainsIgnoreCase(".resources.dll"))
            {
                return true;
            }

            if (_processedAssemblies.Contains(runtimeAssembly.Checksum))
            {
                return true;
            }

            return false;
        }

        protected async Task<List<EmbeddedResource>> FindEmbeddedResourcesAsync(PEReader peReader, RuntimeAssembly containerAssembly)
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
                        ContainerAssembly = containerAssembly,
                        Name = resourceName,
                        Start = resourceStart,
                        Size = size
                    });
                }
            }

            return embeddedResources;
        }

        protected virtual async Task<List<CosturaRuntimeAssembly>> FindEmbeddedAssembliesViaMetadataAsync(IEnumerable<EmbeddedResource> resources)
        {
            var metadataResource = (from x in resources
                                    where x.Name.EqualsIgnoreCase("costura.metadata")
                                    select x).FirstOrDefault();
            if (metadataResource is null)
            {
                // Not found, return null
                return null;
            }

            var embeddedResources = new List<CosturaRuntimeAssembly>();

            unsafe
            {
                using (var resourceStream = new UnmanagedMemoryStream(metadataResource.Start, metadataResource.Size))
                {
#pragma warning disable IDISP001 // Dispose created
                    var streamReader = new StreamReader(resourceStream);
#pragma warning restore IDISP001 // Dispose created

                    while (streamReader.Peek() >= 0)
                    {
#pragma warning disable CL0001 // Use async overload inside this async method
                        var line = streamReader.ReadLine();
#pragma warning restore CL0001 // Use async overload inside this async method
                        if (!string.IsNullOrEmpty(line))
                        {
                            var costuraEmbeddedResource = new CosturaRuntimeAssembly(line);

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
    }
}
