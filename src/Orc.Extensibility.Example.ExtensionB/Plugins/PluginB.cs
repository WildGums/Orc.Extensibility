namespace Orc.Extensibility.Example.ExtensionB.Plugins;

using System.Threading.Tasks;
using System.Windows.Media;
using Catel.Services;
using Services;

public class PluginB : ICustomPlugin
{
    private readonly IMessageService _messageService;
    private readonly IHostService _hostService;
    private readonly ILanguageService _languageService;

    public PluginB(IMessageService messageService, IHostService hostService, ILanguageService languageService)
    {
        _messageService = messageService;
        _hostService = hostService;
        _languageService = languageService;
    }

    public async Task InitializeAsync()
    {
        var value = _languageService.GetRequiredString("TestResource");

        await _messageService.ShowAsync($"Plugin B has been loaded, setting color to green. Resource value: '{value}'");

        _hostService.SetColor(Colors.Green);
    }
}
