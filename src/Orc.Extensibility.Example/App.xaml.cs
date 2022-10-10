namespace Orc.Extensibility.Example
{
    using System;
    using System.Globalization;
    using System.Windows;
    using Catel.Configuration;
    using Catel.IoC;
    using Catel.Logging;
    using Catel.Services;

    using Configuration;
    using Orc.Theming;

    public partial class App
    {
        public App()
        {
#if DEBUG
            LogManager.AddDebugListener(false);
#endif
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            var serviceLocator = ServiceLocator.Default;

            var languageService = serviceLocator.ResolveRequiredType<ILanguageService>();

            // Note: it's best to use .CurrentUICulture in actual apps since it will use the preferred language
            // of the user. But in order to demo multilingual features for devs (who mostly have en-US as .CurrentUICulture),
            // we use .CurrentCulture for the sake of the demo
            languageService.PreferredCulture = CultureInfo.CurrentCulture;
            languageService.FallbackCulture = new CultureInfo("en-US");

            base.OnStartup(e);

            // This shows the StyleHelper, but uses a *copy* of the Orchestra themes. The default margins for controls are not defined in
            // Orc.Theming since it's a low-level library. The final default styles should be in the shell (thus Orchestra makes sense)
            StyleHelper.CreateStyleForwardersForDefaultStyles();

            // Support Costura embedded runtime assemblies
            var appDomainWatcher = serviceLocator.RegisterTypeAndInstantiate<AppDomainRuntimeAssemblyWatcher>();
            if (appDomainWatcher is null)
            {
                throw new InvalidOperationException("Failed to create runtime assembly watcher");
            }

            //appDomainWatcher.AllowAssemblyResolvingFromOtherLoadContexts = false;
            appDomainWatcher.Attach();

            // In an Orchestra environment, this would go into the bootstrapper
            var configurationService = serviceLocator.ResolveRequiredType<IConfigurationService>();
            var activePlugin = await configurationService.GetRoamingValueAsync(ConfigurationKeys.ActivePlugin, ConfigurationKeys.ActivePluginDefaultValue);

            var singlePluginService = serviceLocator.ResolveRequiredType<ISinglePluginService>();
            var plugin = await singlePluginService.ConfigureAndLoadPluginAsync(activePlugin, ConfigurationKeys.ActivePluginDefaultValue);
            if (plugin is not null)
            {
                serviceLocator.RegisterInstance(typeof(ICustomPlugin), plugin.Instance);
            }

            // Watchers
            serviceLocator.RegisterTypeAndInstantiate<RestartRequiredOnPluginChangeConfigurationWatcher>();
        }
    }
}
