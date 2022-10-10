namespace Orc.Extensibility.Example.Services
{
    using System;
    using System.Windows.Media;

    public class ColorEventArgs : EventArgs
    {
        public ColorEventArgs(Color color)
        {
            Color = color;
        }

        public Color Color { get; private set; }
    }
}
