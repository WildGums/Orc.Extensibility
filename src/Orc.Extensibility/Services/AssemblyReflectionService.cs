namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Reflection.PortableExecutable;
    using Catel;
    using Catel.Logging;
    using MethodTimer;
    using Orc.FileSystem;

    public class AssemblyReflectionService : IAssemblyReflectionService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IFileService _fileService;

        private readonly Dictionary<string, bool> _isPeAssembly = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public AssemblyReflectionService(IFileService fileService)
        {
            ArgumentNullException.ThrowIfNull(fileService);

            _fileService = fileService;
        }

#if DEBUG
        [Time("{assemblyPath}")]
#endif
        public virtual bool IsPeAssembly(string assemblyPath)
        {
            if (!_isPeAssembly.TryGetValue(assemblyPath, out var isPeAssembly))
            {
                // Somehow .exe are not pe files
                if (!assemblyPath.EndsWithIgnoreCase(".exe"))
                {
                    using (var fileStream = _fileService.OpenRead(assemblyPath))
                    {
                        isPeAssembly = false;

                        using (var reader = new PEReader(fileStream))
                        {
                            if (reader.HasMetadata)
                            {
                                isPeAssembly = true;
                            }
                        }
                    }
                }

                _isPeAssembly[assemblyPath] = isPeAssembly;
            }

            return isPeAssembly;
        }
    }
}
