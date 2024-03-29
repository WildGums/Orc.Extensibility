﻿namespace Orc.Extensibility;

using System.Threading.Tasks;

public interface ISinglePluginService : IPluginService
{
    Task<IPlugin?> ConfigureAndLoadPluginAsync(string expectedPlugin, string defaultPlugin);
    Task SetFallbackPluginAsync(IPluginInfo? fallbackPlugin);
}