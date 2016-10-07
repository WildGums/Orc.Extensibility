// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestartRequiredOnPluginChangeConfigurationWatcher.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.Configuration
{
    using Catel;
    using Catel.Configuration;
    using Catel.Logging;
    using Catel.Services;

    public class RestartRequiredOnPluginChangeConfigurationWatcher
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IConfigurationService _configurationService;
        private readonly IMessageService _messageService;

        public RestartRequiredOnPluginChangeConfigurationWatcher(IConfigurationService configurationService,
            IMessageService messageService)
        {
            Argument.IsNotNull(() => configurationService);
            Argument.IsNotNull(() => messageService);

            _configurationService = configurationService;
            _messageService = messageService;

            _configurationService.ConfigurationChanged += OnConfigurationServiceConfigurationChanged;
        }

        private async void OnConfigurationServiceConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            if (e.IsConfigurationKey(ConfigurationKeys.ActivePlugin))
            {
                Log.Info("The active plugin has been changed, a restart is required");

                await _messageService.ShowAsync("The active plugin has been changed, a restart is required");
            }
        }
    }
}