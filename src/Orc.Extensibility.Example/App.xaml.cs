// --------------------------------------------------------------------------------------------------------------------
// <copyright file="App.xaml.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example
{
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

            var languageService = serviceLocator.ResolveType<ILanguageService>();

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
            //appDomainWatcher.AllowAssemblyResolvingFromOtherLoadContexts = false;
            appDomainWatcher.Attach();

            // In an Orchestra environment, this would go into the bootstrapper
            var configurationService = serviceLocator.ResolveType<IConfigurationService>();
            var activePlugin = configurationService.GetRoamingValue(ConfigurationKeys.ActivePlugin, ConfigurationKeys.ActivePluginDefaultValue);

            var singlePluginService = serviceLocator.ResolveType<ISinglePluginService>();
            var plugin = await singlePluginService.ConfigureAndLoadPluginAsync(activePlugin, ConfigurationKeys.ActivePluginDefaultValue);
            if (plugin != null)
            {
                serviceLocator.RegisterInstance(typeof(ICustomPlugin), plugin.Instance);
            }

            // Watchers
            serviceLocator.RegisterTypeAndInstantiate<RestartRequiredOnPluginChangeConfigurationWatcher>();
        }
    }
}
