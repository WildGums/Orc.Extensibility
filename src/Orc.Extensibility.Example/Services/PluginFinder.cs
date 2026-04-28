namespace Orc.Extensibility.Example.Services;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Catel;
using Catel.Logging;
using Catel.Reflection;
using FileSystem;
using Microsoft.Extensions.Logging;

public class PluginFinder : PluginFinderBase
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(PluginFinder));

    public PluginFinder(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider, IPluginCleanupService pluginCleanupService,
        IDirectoryService directoryService, IFileService fileService, IAssemblyReflectionService assemblyReflectionService, IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        : base(pluginLocationsProvider, pluginInfoProvider, pluginCleanupService, directoryService, fileService, assemblyReflectionService, runtimeAssemblyResolverService)
    {
    }

    protected override bool IsPlugin(PluginProbingContext context, Type type)
    {
        return type.ImplementsInterfaceEx<ICustomPlugin>();
    }

    protected override Type? GetPluginRegistrar(PluginProbingContext context, Assembly assembly, Type pluginType)
    {
        var pluginRegistrarType = assembly.ExportedTypes.FirstOrDefault(x => x.ImplementsInterfaceEx<ICustomPluginRegistrar>());
        return pluginRegistrarType;
    }

    protected override bool ShouldIgnoreAssembly(string assemblyPath)
    {
        // Since by default, the plugin finder ignores Orc.* assemblies, we need to override it here (ExtensionA and ExtensionB)
        if (assemblyPath.ContainsIgnoreCase("Orc.Extensibility.Example.Extension") &&
            !assemblyPath.ContainsIgnoreCase($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}"))
        {
            return false;
        }

        return base.ShouldIgnoreAssembly(assemblyPath);
    }
}
