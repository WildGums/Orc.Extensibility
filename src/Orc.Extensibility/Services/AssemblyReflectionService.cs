namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using Catel;
using Catel.Logging;
using MethodTimer;
using FileSystem;

public class AssemblyReflectionService : IAssemblyReflectionService
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private readonly IFileService _fileService;

    private readonly Dictionary<string, bool> _isPeAssembly = new(StringComparer.OrdinalIgnoreCase);

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
        if (_isPeAssembly.TryGetValue(assemblyPath, out var isPeAssembly))
        {
            return isPeAssembly;
        }

        // Somehow .exe are not pe files (with .net core)
        if (!assemblyPath.EndsWithIgnoreCase(".exe"))
        {
            using var fileStream = _fileService.OpenRead(assemblyPath);
            isPeAssembly = false;

            using var reader = new PEReader(fileStream);
            if (reader.HasMetadata)
            {
                isPeAssembly = true;
            }
        }

        return isPeAssembly;
    }

    /// <summary>
    /// Allows registering an assembly as PE (or not) to improve performance on PE verification on larger assemblies.
    /// <para />
    /// Note that this method will overwrite already existing determined data if it was determined before.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly.</param>
    /// <param name="isPeAssembly">A value indicating whether the assembly is a PE assembly.</param>
    public virtual void RegisterAssembly(string assemblyPath, bool isPeAssembly)
    {
        _isPeAssembly[assemblyPath] = isPeAssembly;
    }
}
