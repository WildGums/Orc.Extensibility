namespace Orc.Extensibility.Example.ExtensionB.Plugins
{
    using Catel.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Orc.Extensibility.Example.Watchers;

    public class PluginRegistrar : ICustomPluginRegistrar
    {
        public void AddServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddOrcNotifications();

            serviceCollection.AddSingleton<PluginBWatcher>();

            serviceCollection.AddSingleton<ILanguageSource>(new LanguageResourceSource("Orc.Extensibility.Example.ExtensionB", "Orc.Extensibility.Example.Properties", "Resources"));
        }
    }
}
