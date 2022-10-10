namespace Orc.Extensibility.Example
{
    using System.Collections.Generic;
    using Catel.IoC;

    internal static class PluginHelper
    {
        // Note: this class is used as an example to retrieve the active plugins. Best is to create a custom service
        // that will return the active plugins. This way you can choose whether the plugins are registered in the service locator
        // or in a customer service / manager.
        //
        // This class can also easily be modified to switch between single and multiple plugin scenarios

        public static IEnumerable<ICustomPlugin> GetActivePlugins()
        {
            var plugins = new List<ICustomPlugin>();

            var serviceLocator = ServiceLocator.Default;
            if (serviceLocator.IsTypeRegistered<ICustomPlugin>())
            {
                plugins.Add(serviceLocator.ResolveRequiredType<ICustomPlugin>());
            }

            return plugins;
        }
    }
}
