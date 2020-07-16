using Catel.IoC;
using Catel.Services;
using Orc.Extensibility;

/// <summary>
/// Used by the ModuleInit. All code inside the Initialize method is ran as soon as the assembly is loaded.
/// </summary>
public static class ModuleInitializer
{
    /// <summary>
    /// Initializes the module.
    /// </summary>
    public static void Initialize()
    {
        var serviceLocator = ServiceLocator.Default;

        serviceLocator.RegisterType<IPluginCleanupService, PluginCleanupService>();
        serviceLocator.RegisterType<IPluginLocationsProvider, PluginLocationsProvider>();
        serviceLocator.RegisterType<IPluginManager, PluginManager>();
        serviceLocator.RegisterType<IPluginFactory, PluginFactory>();
        serviceLocator.RegisterType<IPluginInfoProvider, PluginInfoProvider>();

        serviceLocator.RegisterType<IRuntimeAssemblyResolverService, RuntimeAssemblyResolverService>();
        serviceLocator.RegisterType<IAssemblyReflectionService, AssemblyReflectionService>();

        serviceLocator.RegisterType<ILoadedPluginService, LoadedPluginService>();
        serviceLocator.RegisterType<ISinglePluginService, SinglePluginService>();
        serviceLocator.RegisterType<IMultiplePluginsService, MultiplePluginsService>();

        var languageService = serviceLocator.ResolveType<ILanguageService>();
        languageService.RegisterLanguageSource(new LanguageResourceSource("Orc.Extensibility", "Orc.Extensibility.Properties", "Resources"));
    }
}
