namespace Orc.Extensibility.Example.Configuration;

using System;
using Catel.Configuration;
using Catel.IoC;
using Catel.Logging;
using Catel.Services;
using Microsoft.Extensions.Logging;

public class RestartRequiredOnPluginChangeConfigurationWatcher : IConstructAtStartup
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(RestartRequiredOnPluginChangeConfigurationWatcher));

    private readonly IConfigurationService _configurationService;
    private readonly IMessageService _messageService;

    public RestartRequiredOnPluginChangeConfigurationWatcher(IConfigurationService configurationService,
        IMessageService messageService)
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(messageService);

        _configurationService = configurationService;
        _messageService = messageService;

        _configurationService.ConfigurationChanged += OnConfigurationServiceConfigurationChanged;
    }

#pragma warning disable AvoidAsyncVoid
    private async void OnConfigurationServiceConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
#pragma warning restore AvoidAsyncVoid
    {
        if (e.IsConfigurationKey(ConfigurationKeys.ActivePlugin))
        {
            Logger.LogInformation("The active plugin has been changed, a restart is required");

            await _messageService.ShowAsync("The active plugin has been changed, a restart is required");
        }
    }
}
