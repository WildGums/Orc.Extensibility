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

    public class RuntimeAssemblyResolverService : IRuntimeAssemblyResolverService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;
        private readonly IAssemblyReflectionService _assemblyReflectionService;
        private readonly IAppDataService _appDataService;

        private readonly string _embeddedAssembliesTargetDirectory;
        private readonly Dictionary<string, RuntimeAssembly> _extractedAssemblies = new Dictionary<string, RuntimeAssembly>(StringComparer.OrdinalIgnoreCase);

        public RuntimeAssemblyResolverService(IFileService fileService, IDirectoryService directoryService, IAssemblyReflectionService assemblyReflectionService, IAppDataService appDataService)
        {
            _fileService = fileService;
            _directoryService = directoryService;
            _assemblyReflectionService = assemblyReflectionService;
            _appDataService = appDataService;

            _embeddedAssembliesTargetDirectory = DetermineTargetDirectory();

            _directoryService.Delete(_embeddedAssembliesTargetDirectory, true);
            _directoryService.Create(_embeddedAssembliesTargetDirectory);
        }

        public RuntimeAssembly[] GetRuntimeAssemblies()
        {
            return _extractedAssemblies.Values.ToArray();
        }

        public void RegisterAssembly(string assemblyLocation)
        {
            // TODO: We *could* consider lazy-loading, but let's pre-load for now
            UnpackCosturaEmbeddedAssemblies(assemblyLocation, _embeddedAssembliesTargetDirectory);
        }

        protected virtual string DetermineTargetDirectory()
        {
            return System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(Catel.IO.ApplicationDataTarget.UserLocal), "runtime");
        }

        protected virtual void UnpackCosturaEmbeddedAssemblies(string assemblyPath, string targetDirectory)
        {
            Log.Debug($"Unpacking all Costura embedded assemblies from '{assemblyPath}' to '{targetDirectory}'");

            using (var fileStream = _fileService.OpenRead(assemblyPath))
            {
                using (var peReader = new PEReader(fileStream))
                {
                    var resourcesDirectory = peReader.PEHeaders.CorHeader.ResourcesDirectory;
                    if (resourcesDirectory.Size <= 0)
                    {
                        return;
                    }

                    if (!peReader.PEHeaders.TryGetDirectoryOffset(resourcesDirectory, out var start))
                    {
                        Log.Warning($"could not obtain ResourcesDirectory offset for assembly {assemblyPath}");
                        return;
                    }

                    var peImage = peReader.GetEntireImage();
                    if (start + resourcesDirectory.Size >= peImage.Length)
                    {
                        Log.Warning($"Invalid resource offset {start} + length {resourcesDirectory.Size} greater than {peImage.Length}");
                        return;
                    }

                    unsafe
                    {
                        byte* resourcesStart = peImage.Pointer + start;

                        var mdReader = peReader.GetMetadataReader();

                        foreach (var resourceHandle in mdReader.ManifestResources)
                        {
                            var resource = mdReader.GetManifestResource(resourceHandle);

                            // Only care about embedded resources
                            if (!resource.Implementation.IsNil)
                            {
                                continue;
                            }

                            // Only care about costura
                            var resourceName = mdReader.GetString(resource.Name);
                            if (!resourceName.StartsWithIgnoreCase("costura.") || resourceName.Contains(".pdb"))
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

                            byte* resourceStart = resourcesStart + resource.Offset + sizeof(int);
                            using (var resourceStream = new UnmanagedMemoryStream(resourceStart, size))
                            {
                                ExtractAssemblyFromEmbeddedResource(assemblyPath, resourceStream, resourceName, targetDirectory);
                            }
                        }
                    }
                }
            }
        }

        protected virtual void ExtractAssemblyFromEmbeddedResource(string sourceAssemblyPath, Stream stream, string resourceName, string targetDirectory)
        {
            if (resourceName.Contains(".pdb"))
            {
                // Ignore for now
                return;
            }

            var assemblyName = resourceName.Replace("costura.", string.Empty)
                .Replace(".compressed", string.Empty)
                .Replace(".dll", string.Empty)
                .Replace(".exe", string.Empty);

            if (_extractedAssemblies.ContainsKey(assemblyName))
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

            using (var assemblyStream = LoadStream(stream, resourceName))
            {
                if (assemblyStream is null)
                {
                    return;
                }

                rawAssembly = ReadStream(assemblyStream);

                // Note: in the future, we could use reflection to check the version and see if it's higher, if so, use that instead

                var targetFileName = Path.Combine(targetDirectory, assemblyName);

                using (var targetStream = _fileService.Create(targetFileName))
                {
                    targetStream.Write(rawAssembly, 0, rawAssembly.Length);
                    targetStream.Flush();
                }

                _extractedAssemblies.Add(assemblyName, new RuntimeAssembly(assemblyName, targetFileName, sourceAssemblyPath));

                // Could be nested, extract this one
                RegisterAssembly(targetFileName);
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
    }
}
