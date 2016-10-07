﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextBoxLogListener.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.Logging
{
    using System;
    using System.Windows.Controls;
    using Catel;
    using Catel.Logging;

    public class TextBoxLogListener : LogListenerBase
    {
        #region Fields
        private readonly TextBox _textBox;
        #endregion

        #region Constructors
        public TextBoxLogListener(TextBox textBox)
        {
            Argument.IsNotNull(() => textBox);

            _textBox = textBox;

            IgnoreCatelLogging = true;
        }
        #endregion

        #region Methods
        public void Clear()
        {
            _textBox.Dispatcher.Invoke(new Action(() => _textBox.Clear()));
        }

        protected override void Write(ILog log, string message, LogEvent logEvent, object extraData, LogData logData, DateTime time)
        {
            _textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.AppendText(string.Format("{0} [{1}] {2}", time.ToString("hh:mm:ss.fff"), logEvent.ToString().ToUpper(), message));
                _textBox.AppendText(Environment.NewLine);
                _textBox.ScrollToEnd();
            }));
        }
        #endregion
    }
}