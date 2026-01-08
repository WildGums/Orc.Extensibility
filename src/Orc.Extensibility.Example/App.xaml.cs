namespace Orc.Extensibility.Example;

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using Catel;
using Catel.Collections;
using Catel.Configuration;
using Catel.IoC;
using Catel.Services;
using Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orc.Extensibility.Example.Services;
using Orc.Extensibility.Example.Views;
using Orchestra;

public partial class App : Application
{
#pragma warning disable IDISP006 // Implement IDisposable
    private IHost? _host;
#pragma warning restore IDISP006 // Implement IDisposable

    public App()
    {
    }

    private async Task InitializeApplicationAsync()
    {
        // Step 1: Find the plugins (in a simplified service provider, must be as fast as possible)
        //
        // Things to note:
        // * In a real app, there should be some sort of fail-safe

        var pluginProbingServiceCollection = new ServiceCollection();

        pluginProbingServiceCollection.AddCatelCore();
        pluginProbingServiceCollection.AddOrcExtensibility(x =>
        {
            x.EnableSinglePluginService = true;
            x.EnableCosturaSupport = true;
        });
        pluginProbingServiceCollection.AddOrcFileSystem();
        pluginProbingServiceCollection.AddOrchestraCore();

        pluginProbingServiceCollection.AddSingleton<IHostService, HostService>();
        pluginProbingServiceCollection.AddSingleton<IPluginFinder, PluginFinder>();

        pluginProbingServiceCollection.AddLogging(x =>
        {
            x.AddConsole();
            x.AddDebug();
        });

        using var pluginProbingServiceProvider = pluginProbingServiceCollection.BuildServiceProvider();

        var pluginFinder = pluginProbingServiceProvider.GetRequiredService<IPluginFinder>();
        var plugins = await pluginFinder.FindPluginsAsync();

        // Find plugin registrars

        var pluginServiceCollection = new ServiceCollection();

        var pluginFactory = pluginProbingServiceProvider.GetRequiredService<IPluginFactory>();

        foreach (var plugin in plugins)
        {
            var pluginRegistrarTypeInfo = plugin.PluginRegistrar;
            if (pluginRegistrarTypeInfo is null)
            {
                continue;
            }

            var pluginRegistrar = pluginFactory.CreatePluginType(pluginRegistrarTypeInfo) as ICustomPluginRegistrar;
            if (pluginRegistrar is null)
            {
                continue;
            }

            pluginRegistrar.AddServices(pluginServiceCollection);
        }

        // Step 2: Start the app now we know what plugins there are, allow
        // all of them to initialize
        var hostBuilder = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                // Clone all
                pluginServiceCollection.ForEach(x => services.Add(x));

                services.AddCatelCore();
                services.AddCatelMvvm();
                services.AddOrcAutomation();
                services.AddOrcControls();
                services.AddOrcExtensibility(x =>
                {
                    x.EnableSinglePluginService = true;
                    x.EnableCosturaSupport = true;
                });
                services.AddOrcFileSystem();
                services.AddOrcLogViewer();
                services.AddOrcSerializationJson();
                services.AddOrcSystemInfo();
                services.AddOrcTheming();
                services.AddOrchestraCore();

                services.AddSingleton<IHostService, HostService>();
                services.AddSingleton<IPluginFinder, PluginFinder>();

                services.AddSingleton<RestartRequiredOnPluginChangeConfigurationWatcher>();

                services.AddLogging(x =>
                {
                    x.AddConsole();
                    x.AddDebug();
                });
            });

        _host?.Dispose();
        _host = hostBuilder.Build();

        IoCContainer.ServiceProvider = _host.Services;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await InitializeApplicationAsync();

        base.OnStartup(e);

        var serviceProvider = IoCContainer.ServiceProvider;

        serviceProvider.CreateTypesThatMustBeConstructedAtStartup();

        var languageService = serviceProvider.GetRequiredService<ILanguageService>();

        // Note: it's best to use .CurrentUICulture in actual apps since it will use the preferred language
        // of the user. But in order to demo multilingual features for devs (who mostly have en-US as .CurrentUICulture),
        // we use .CurrentCulture for the sake of the demo
        languageService.PreferredCulture = CultureInfo.CurrentCulture;
        languageService.FallbackCulture = new CultureInfo("en-US");

        this.ApplyTheme();

        // In an Orchestra environment, this would go into the bootstrapper
        var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        await configurationService.LoadAsync();
        var activePlugin = configurationService.GetRoamingValue(ConfigurationKeys.ActivePlugin, ConfigurationKeys.ActivePluginDefaultValue);

        var singlePluginService = serviceProvider.GetRequiredService<ISinglePluginService>();
        var plugin = await singlePluginService.ConfigureAndLoadPluginAsync(activePlugin, ConfigurationKeys.ActivePluginDefaultValue);

        var mainWindow = ActivatorUtilities.CreateInstance<MainWindow>(_host!.Services);
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        using (_host)
        {
            _ = _host?.StopAsync();
        }

        base.OnExit(e);
    }
}
