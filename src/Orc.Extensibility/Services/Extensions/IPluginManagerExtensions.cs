namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Catel.Logging;

    public static class IPluginManagerExtensions
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public static async Task<IEnumerable<IPluginInfo>> RefreshAndGetPluginsAsync(this IPluginManager pluginManager)
        {
            ArgumentNullException.ThrowIfNull(pluginManager);

            await pluginManager.RefreshAsync();

            return pluginManager.GetPlugins();
        }

        //public static List<IPluginInfo> FindPluginImplementations<TPlugin>(this IPluginManager pluginManager)
        //{
        //    return FindPluginImplementations(pluginManager, typeof(TPlugin));
        //}

        //public static List<IPluginInfo> FindPluginImplementations(this IPluginManager pluginManager, Type interfaceType)
        //{
        //    ArgumentNullException.ThrowIfNull(interfaceType);

        //    var interfaceTypeFullName = interfaceType.FullName;
        //    var plugins = new List<IPluginInfo>();

        //    Log.Debug("Searching for plugins that implement '{0}'", interfaceType.FullName);

        //    foreach (var plugin in pluginManager.GetPlugins())
        //    {
        //        // Note: look at reflection-only interfaces
        //        var isValidPlugin = (from iface in plugin.ReflectionOnlyType.GetInterfacesEx()
        //                             where string.Equals(interfaceTypeFullName, iface.FullName)
        //                             select iface).Any();

        //        if (isValidPlugin)
        //        {
        //            plugins.Add(plugin);
        //        }
        //    }

        //    return plugins;
        //}
    }
}
