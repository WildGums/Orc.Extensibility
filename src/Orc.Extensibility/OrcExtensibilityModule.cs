namespace Orc
{
    using Catel.Services;
    using Catel.ThirdPartyNotices;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Orc.Extensibility;

    /// <summary>
    /// Core module which allows the registration of default services in the service collection.
    /// </summary>
    public static class OrcExtensibilityModule
    {
        public static IServiceCollection AddOrcExtensibility(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IPluginCleanupService, PluginCleanupService>();
            serviceCollection.TryAddSingleton<IPluginLocationsProvider, PluginLocationsProvider>();
            serviceCollection.TryAddSingleton<IPluginManager, PluginManager>();
            serviceCollection.TryAddSingleton<IPluginFactory, PluginFactory>();
            serviceCollection.TryAddSingleton<IPluginInfoProvider, PluginInfoProvider>();

            serviceCollection.TryAddSingleton<IRuntimeAssemblyResolverService, RuntimeAssemblyResolverService>();
            serviceCollection.TryAddSingleton<IAssemblyReflectionService, AssemblyReflectionService>();

            serviceCollection.TryAddSingleton<ILoadedPluginService, LoadedPluginService>();
            serviceCollection.TryAddSingleton<ISinglePluginService, SinglePluginService>();
            serviceCollection.TryAddSingleton<IMultiplePluginsService, MultiplePluginsService>();

            serviceCollection.AddSingleton<ILanguageSource>(new LanguageResourceSource("Orc.Extensibility", "Orc.Extensibility.Properties", "Resources"));

            serviceCollection.AddSingleton<IThirdPartyNotice>((x) => new LibraryThirdPartyNotice("Orc.Extensibility", "https://github.com/wildgums/orc.extensibility"));

            return serviceCollection;
        }
    }
}
