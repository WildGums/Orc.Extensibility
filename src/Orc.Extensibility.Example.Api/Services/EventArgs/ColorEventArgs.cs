// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorEventARgs.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


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