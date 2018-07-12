// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginB.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.ExtensionB.Plugins
{
    using System.Threading.Tasks;
    using System.Windows.Media;
    using Catel;
    using Catel.Services;
    using Services;

    public class PluginB : ICustomPlugin
    {
        private readonly IMessageService _messageService;
        private readonly IHostService _hostService;

        public PluginB(IMessageService messageService, IHostService hostService)
        {
            Argument.IsNotNull(() => messageService);
            Argument.IsNotNull(() => hostService);

            _messageService = messageService;
            _hostService = hostService;
        }

        public async Task InitializeAsync()
        {
            await _messageService.ShowAsync("Plugin B has been loaded, setting color to green");

            _hostService.SetColor(Colors.Green);
        }
    }
}