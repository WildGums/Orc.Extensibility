// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility.Example.Views
{
    using Catel.Logging;
    using Example.Logging;

    public partial class MainWindow
    {
        #region Constructors
        public MainWindow()
        {
            InitializeComponent();

            var logListener = new TextBoxLogListener(loggingTextBox)
            {
                IgnoreCatelLogging = true
            };

            LogManager.AddListener(logListener);
        }
        #endregion
    }
}