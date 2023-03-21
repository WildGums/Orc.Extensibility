namespace Orc.Extensibility.Example.Logging;

using System;
using System.Windows.Controls;
using Catel.Logging;

public class TextBoxLogListener : LogListenerBase
{
    private readonly TextBox _textBox;

    public TextBoxLogListener(TextBox textBox)
    {
        ArgumentNullException.ThrowIfNull(textBox);

        _textBox = textBox;

        IgnoreCatelLogging = true;
    }

    public void Clear()
    {
        _textBox.Dispatcher.Invoke(new Action(() => _textBox.Clear()));
    }

    protected override void Write(ILog log, string message, LogEvent logEvent, object? extraData, LogData? logData, DateTime time)
    {
        _textBox.Dispatcher.BeginInvoke(new Action(() =>
        {
            _textBox.AppendText($"{time.ToString("hh:mm:ss.fff")} [{logEvent.ToString().ToUpper()}] {message}");
            _textBox.AppendText(Environment.NewLine);
            _textBox.ScrollToEnd();
        }));
    }
}
