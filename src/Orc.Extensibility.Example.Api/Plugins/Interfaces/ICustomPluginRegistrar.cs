namespace Orc.Extensibility.Example;

using Microsoft.Extensions.DependencyInjection;

public interface ICustomPluginRegistrar
{
    void AddServices(IServiceCollection serviceCollection);
}
