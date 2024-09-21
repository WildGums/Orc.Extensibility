namespace Orc.Extensibility.Example.ExtensionA.Plugins;

using System.Threading.Tasks;
using Catel.Logging;
using Catel.Services;
using Services;

public class PluginA : ICustomPlugin
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private readonly IMessageService _messageService;
    private readonly IHostService _hostService;
    private readonly ILanguageService _languageService;

    public PluginA(IMessageService messageService, IHostService hostService, ILanguageService languageService)
    {
        _messageService = messageService;
        _hostService = hostService;
        _languageService = languageService;
    }

    public async Task InitializeAsync()
    {
        var value = _languageService.GetRequiredString("TestResource");

        Log.Info($"{typeof(YamlDotNet.Core.AnchorName).FullName}");
        Log.Info($"{typeof(System.Data.SqlClient.SqlClientFactory).FullName}");
        Log.Info($"{typeof(Microsoft.Data.SqlClient.SqlClientFactory).FullName}");
        Log.Info($"{typeof(Microsoft.Data.SqlClient.SqlConnection).FullName}");

        try
        {
            var connectionString = ".\\SQLExpress";

            System.Data.Entity.Database.Exists(connectionString);

            //using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
            //{
            //    await connection.OpenAsync();
            //}
        }
        catch 
        {
            // Ignore
            Log.Error("Failed to open database connection");
        }

        await _messageService.ShowAsync($"Plugin A has been loaded, setting color to red. Resource value: '{value}'");

        _hostService.SetColor(System.Windows.Media.Colors.Red);
    }
}
