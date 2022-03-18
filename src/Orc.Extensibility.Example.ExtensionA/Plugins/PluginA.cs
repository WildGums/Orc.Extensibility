// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginA.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.ExtensionA.Plugins
{
    using System.Threading.Tasks;
    using Catel;
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
            var value = _languageService.GetString("TestResource");

            Log.Info($"{typeof(YamlDotNet.Core.AnchorName).FullName}");

            await _messageService.ShowAsync($"Plugin A has been loaded, setting color to red. Resource value: '{value}'");

            _hostService.SetColor(System.Windows.Media.Colors.Red);
        }
    }
}
