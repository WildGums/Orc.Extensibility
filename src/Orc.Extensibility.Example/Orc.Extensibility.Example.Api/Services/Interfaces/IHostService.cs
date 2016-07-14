// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IHostService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.Services
{
    using System;
    using System.Windows.Media;

    public interface IHostService
    {
        event EventHandler<ColorEventArgs> ColorChanged;

        void SetColor(Color color);
    }
}