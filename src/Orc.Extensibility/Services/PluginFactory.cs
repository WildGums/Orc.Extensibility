namespace Orc.Extensibility
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Catel;
    using Catel.IoC;
    using Catel.Logging;
    using Catel.Reflection;
    using MethodTimer;

#if NETCORE
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
#endif

    public class PluginFactory : IPluginFactory
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly ITypeFactory _typeFactory;
        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;

        private PropertyInfo _runtimeTypePropertyInfo;

        public PluginFactory(ITypeFactory typeFactory, IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        {
            Argument.IsNotNull(() => typeFactory);
            Argument.IsNotNull(() => runtimeAssemblyResolverService);

            _typeFactory = typeFactory;
            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
        }

        [Time]
        public virtual object CreatePlugin(IPluginInfo pluginInfo)
        {
            Argument.IsNotNull(() => pluginInfo);

            try
            {
                Log.Debug($"Creating plugin '{pluginInfo}'");

                Log.Debug($"  1. Loading assembly from '{pluginInfo.Location}'");

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
                var assembly = Assembly.LoadFrom(pluginInfo.Location);

#if NETCORE
                //// NOTE: when using separate load context per assembly, this becomes important
                //var loadContext = AssemblyLoadContext.GetLoadContext(assembly);
                //loadContext.Resolving += OnLoadContextResolving;
#endif

                Log.Debug($"  2. Getting type '{pluginInfo.FullTypeName}' from loaded assembly");

                var type = assembly.GetType(pluginInfo.FullTypeName);

                Log.Debug($"  3. Force loading assembly into AppDomain (if using Fody.ModuleInit)");

                try
                {
                    PreloadAssembly(assembly);
                }
                catch (Exception innerEx)
                {
                    Log.Warning(innerEx, "Failed to preload assembly");
                }

                Log.Debug($"  4. Instantiating type '{type.GetSafeFullName(true)}'");

                var plugin = _typeFactory.CreateInstance(type);

                Log.Debug($"Plugin creation resulted in an instance: '{plugin is not null}'");

                // Workaround for loading assemblies
                TypeCache.InitializeTypes(type.GetAssemblyEx(), true);

                return plugin;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create plugin '{pluginInfo}'");

                throw;
            }
        }

        protected virtual void PreloadAssembly(Assembly assembly)
        {
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
                            Log.Debug("Found module runtime type, force preloading assembly now");

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
}
