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
    private readonly ILanguageService _languageService;

    public RestartRequiredOnPluginChangeConfigurationWatcher(IConfigurationService configurationService,
        IMessageService messageService, ILanguageService languageService)
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(messageService);
        ArgumentNullException.ThrowIfNull(languageService);

        _configurationService = configurationService;
        _messageService = messageService;
        _languageService = languageService;

        _configurationService.ConfigurationChanged += OnConfigurationServiceConfigurationChanged;
    }

#pragma warning disable AvoidAsyncVoid
    private async void OnConfigurationServiceConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
#pragma warning restore AvoidAsyncVoid
    {
        if (e.IsConfigurationKey(ConfigurationKeys.ActivePlugin))
        {
            var message = _languageService.GetString("RestartRequiredOnPluginChangeConfigurationWatcher_PluginChangedMessage")
                ?? "The active plugin has been changed, a restart is required";

            Logger.LogInformation(message);

            await _messageService.ShowAsync(message);
        }
    }
}
