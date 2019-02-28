// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HostLoggingService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.Services
{
    using System;
    using System.Windows.Media;
    using Catel;
    using Catel.Logging;

    public class HostService : IHostService
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public event EventHandler<ColorEventArgs> ColorChanged;

        public void SetColor(Color color)
        {
            Log.Info($"Changing color to '{color}'");

            ColorChanged?.Invoke(this, new ColorEventArgs(color));
        }
    }
}
