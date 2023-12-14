namespace Orc.Extensibility.Example.Views;

using Catel.Logging;
using Logging;

public partial class MainWindow
{ public MainWindow()
    {
        InitializeComponent();

        var logListener = new TextBoxLogListener(loggingTextBox)
        {
            IgnoreCatelLogging = true
        };

        LogManager.AddListener(logListener);
    }
}
