namespace Orc.Extensibility.Example.Services;

using System;
using System.Windows.Media;
using Catel.Logging;
using Microsoft.Extensions.Logging;

public class HostService : IHostService
{
    private static readonly ILogger Logger = LogManager.GetLogger(typeof(HostService));

    public event EventHandler<ColorEventArgs>? ColorChanged;

    public void SetColor(Color color)
    {
        Logger.LogInformation($"Changing color to '{color}'");

        ColorChanged?.Invoke(this, new ColorEventArgs(color));
    }
}
