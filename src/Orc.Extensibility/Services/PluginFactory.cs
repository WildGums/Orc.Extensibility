namespace Orc.Extensibility;

using System;
using System.Linq;
using System.Reflection;
using Catel.IoC;
using Catel.Logging;
using Catel.Reflection;
using MethodTimer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class PluginFactory : IPluginFactory
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(PluginFactory));

    private readonly IServiceProvider _serviceProvider;
    private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;

    private PropertyInfo? _runtimeTypePropertyInfo;

    public PluginFactory(IServiceProvider serviceProvider, IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(runtimeAssemblyResolverService);

        _serviceProvider = serviceProvider;
        _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
    }

    [Time]
    public virtual object CreatePluginType(IPluginTypeInfo pluginTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(pluginTypeInfo);

        try
        {
            Logger.LogDebug($"Creating plugin '{pluginTypeInfo}'");

            Logger.LogDebug($"  1. Loading assembly from '{pluginTypeInfo.Location}'");

            //#if NETCORE
            //                // Use DotNetCorePlugins
            //                var pluginLoader = PluginLoader.CreateFromAssemblyFile(
            //                    assemblyFile: pluginInfo.Location,
            //                    x =>
            //                    {
            //                        // See https://github.com/natemcmaster/DotNetCorePlugins/blob/main/docs/what-are-shared-types.md
            //                        x.PreferSharedTypes = true;
            //                        x.AdditionalProbingPaths.Add(_runtimeAssemblyResolverService.TargetDirectory);
            //                    });
            //                var assembly = pluginLoader.LoadDefaultAssembly();
            //#else

            // Note: load via assembly name does not work when it's in a specific directory in .net core
            //var assemblyName = AssemblyName.GetAssemblyName(pluginInfo.Location);
            //var assembly = Assembly.Load(assemblyName);
            var assembly = Assembly.LoadFrom(pluginTypeInfo.Location);

            //// NOTE: when using separate load context per assembly, this becomes important
            //var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
            //loadContext.Resolving += OnLoadContextResolving;

            Logger.LogDebug($"  2. Getting type '{pluginTypeInfo.FullTypeName}' from loaded assembly");

            var type = assembly.GetType(pluginTypeInfo.FullTypeName);
            if (type is null)
            {
                throw Logger.LogErrorAndCreateException<NotSupportedException>($"Cannot find type '{pluginTypeInfo.FullTypeName}'");
            }

            Logger.LogDebug($"  3. Force loading assembly into AppDomain (if using Fody.ModuleInit)");

            try
            {
                PreloadAssembly(assembly);
            }
            catch (Exception innerEx)
            {
                Logger.LogWarning(innerEx, "Failed to preload assembly");
            }

            Logger.LogDebug($"  4. Instantiating type '{type.GetSafeFullName(true)}'");

            var plugin = ActivatorUtilities.CreateInstance(_serviceProvider, type);

            // Workaround for loading assemblies
            TypeCache.InitializeTypes(type.GetAssemblyEx(), true);

            return plugin;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to create plugin '{pluginTypeInfo}'");

            throw;
        }
    }

    protected virtual void PreloadAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        // This specific preload code is written to allow module initializers (e.g. Fody.ModuleInit) to run *before* creating the plugin. This
        // will allow an assembly to register services *before* the constructor is invoked and allows for dependency injection of plugins, even
        // if the types are coming from the same plugin

        var modules = assembly.GetModules();
        if (modules.Length > 0)
        {
            var firstModule = modules.FirstOrDefault();
            if (firstModule is not null)
            {
                if (_runtimeTypePropertyInfo is null)
                {
                    _runtimeTypePropertyInfo = firstModule.GetType().GetPropertyEx("RuntimeType");
                }

                if (_runtimeTypePropertyInfo is not null)
                {
                    var runtimeType = _runtimeTypePropertyInfo.GetValue(firstModule) as Type;
                    if (runtimeType is not null)
                    {
                        Logger.LogDebug("Found module runtime type, force preloading assembly now");

                        var staticConstructor = runtimeType.GetConstructor(BindingFlags.Static | BindingFlags.NonPublic, Type.DefaultBinder, Array.Empty<Type>(), Array.Empty<ParameterModifier>());
                        if (staticConstructor is not null)
                        {
                            staticConstructor.Invoke(null, Array.Empty<object>());
                        }
                    }
                }
            }
        }
    }
}
