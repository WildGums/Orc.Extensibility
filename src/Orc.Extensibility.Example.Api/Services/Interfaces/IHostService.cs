namespace Orc.Extensibility.Example.Services;

using System;
using System.Windows.Media;

public interface IHostService
{
    event EventHandler<ColorEventArgs>? ColorChanged;

    void SetColor(Color color);
}
