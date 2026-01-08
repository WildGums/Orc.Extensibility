namespace Orc
{
    using System;
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
            return AddOrcExtensibility(serviceCollection, null);
        }

        public static IServiceCollection AddOrcExtensibility(this IServiceCollection serviceCollection,
            Action<OrcExtensibilityConfig>? configure)
        {
            var config = new OrcExtensibilityConfig();

            configure?.Invoke(config);

            if (!config.EnableSinglePluginService &&
                !config.EnableMultiplePluginsService)
            {
                throw new NotSupportedException("At least 1 plugin service should be enabled via settings");
            }

            serviceCollection.TryAddSingleton<IPluginCleanupService, PluginCleanupService>();
            serviceCollection.TryAddSingleton<IPluginLocationsProvider, PluginLocationsProvider>();
            serviceCollection.TryAddSingleton<IPluginManager, PluginManager>();
            serviceCollection.TryAddSingleton<IPluginFactory, PluginFactory>();
            serviceCollection.TryAddSingleton<IPluginInfoProvider, PluginInfoProvider>();

            serviceCollection.TryAddSingleton<IRuntimeAssemblyResolverService, RuntimeAssemblyResolverService>();
            serviceCollection.TryAddSingleton<IAssemblyReflectionService, AssemblyReflectionService>();

            serviceCollection.TryAddSingleton<ILoadedPluginService, LoadedPluginService>();

            if (config.EnableSinglePluginService)
            {
                serviceCollection.TryAddSingleton<ISinglePluginService, SinglePluginService>();
            }

            if (config.EnableMultiplePluginsService)
            {
                serviceCollection.TryAddSingleton<IMultiplePluginsService, MultiplePluginsService>();
            }

            if (config.EnableCosturaSupport)
            {
                serviceCollection.TryAddSingleton<AppDomainRuntimeAssemblyWatcher>();
            }

            serviceCollection.AddSingleton<ILanguageSource>(new LanguageResourceSource("Orc.Extensibility", "Orc.Extensibility.Properties", "Resources"));

            serviceCollection.AddSingleton<IThirdPartyNotice>((x) => new LibraryThirdPartyNotice("Orc.Extensibility", "https://github.com/wildgums/orc.extensibility"));

            return serviceCollection;
        }
    }

    public class OrcExtensibilityConfig
    {
        public OrcExtensibilityConfig()
        {
            // Fastest options enabled by default
            EnableSinglePluginService = true;
            EnableMultiplePluginsService = false;
            EnableCosturaSupport = false;
        }

        public bool EnableSinglePluginService { get; set; }

        public bool EnableMultiplePluginsService { get; set; }

        public bool EnableCosturaSupport { get; set; }
    }
}
