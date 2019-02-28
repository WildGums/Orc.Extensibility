// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginA.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.ExtensionA.Plugins
{
    using System.Threading.Tasks;
    using Catel;
    using Catel.Services;
    using Services;

    public class PluginA : ICustomPlugin
    {
        private readonly IMessageService _messageService;
        private readonly IHostService _hostService;

        public PluginA(IMessageService messageService, IHostService hostService)
        {
            Argument.IsNotNull(() => messageService);
            Argument.IsNotNull(() => hostService);

            _messageService = messageService;
            _hostService = hostService;
        }

        public async Task InitializeAsync()
        {
            await _messageService.ShowAsync("Plugin A has been loaded, setting color to red");

            _hostService.SetColor(System.Windows.Media.Colors.Red);
        }
    }
}
