namespace Orc.Extensibility
{
    using System;
    using Catel.IO;

    public static class PluginExtensions
    {
        public static string GetAssemblyName(this IPluginInfo pluginInfo)
        {
            ArgumentNullException.ThrowIfNull(pluginInfo);

            return Path.GetFileName(pluginInfo.Location);
        }
    }
}
