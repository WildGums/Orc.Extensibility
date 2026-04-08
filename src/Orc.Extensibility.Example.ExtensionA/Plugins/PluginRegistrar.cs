namespace Orc.Extensibility.Example.ExtensionA.Plugins;

using Catel.Services;
using Microsoft.Extensions.DependencyInjection;

public class PluginRegistrar : ICustomPluginRegistrar
{
    public void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ILanguageSource>(new LanguageResourceSource("Orc.Extensibility.Example.ExtensionA", "Orc.Extensibility.Example.Properties", "Resources"));
    }
}
