// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.ViewModels
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using Catel;
    using Catel.Configuration;
    using Catel.Logging;
    using Catel.MVVM;
    using Catel.Services;
    using Example.Services;
    using Orc.Extensibility;

    public class MainViewModel : ViewModelBase
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly IHostService _hostService;
        private readonly IDispatcherService _dispatcherService;
        private readonly IPluginManager _pluginManager;
        private readonly IConfigurationService _configurationService;
        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;

        private bool _isInitialized;

        public MainViewModel(IHostService hostService, IDispatcherService dispatcherService, IPluginManager pluginManager,
            IConfigurationService configurationService, IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        {
            Argument.IsNotNull(() => hostService);
            Argument.IsNotNull(() => dispatcherService);
            Argument.IsNotNull(() => pluginManager);
            Argument.IsNotNull(() => configurationService);
            Argument.IsNotNull(() => runtimeAssemblyResolverService);

            _hostService = hostService;
            _dispatcherService = dispatcherService;
            _pluginManager = pluginManager;
            _configurationService = configurationService;
            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;

            Title = "Orc.Extensibility example";
        }

        #region Properties
        public List<IPluginInfo> AvailablePlugins { get; private set; }

        public IPluginInfo SelectedPlugin { get; set; }

        public List<RuntimeAssembly> RuntimeAssemblies { get; private set; }

        public Color Color { get; private set; }
        #endregion

        #region Methods
        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var selectedPlugin = _configurationService.GetRoamingValue(ConfigurationKeys.ActivePlugin, ConfigurationKeys.ActivePluginDefaultValue);

            AvailablePlugins = _pluginManager.GetPlugins().ToList();
            SelectedPlugin = (from plugin in AvailablePlugins
                              where plugin.FullTypeName.Contains(selectedPlugin)
                              select plugin).FirstOrDefault();

            _hostService.ColorChanged += OnHostServiceColorChanged;

            // In an orchestra environment, this could go into the bootstrappers

            Log.Info("Initializing plugins");

            foreach (var plugin in PluginHelper.GetActivePlugins())
            {
                Log.Info($"Initializing plugin '{plugin.GetType().Name}'");

                await plugin.InitializeAsync();
            }

            RuntimeAssemblies = (from pluginLoadContext in _runtimeAssemblyResolverService.GetPluginLoadContexts()
                                 from runtimeAssembly in pluginLoadContext.RuntimeAssemblies
                                 select runtimeAssembly).ToList();

            _isInitialized = true;
        }

        protected override async Task CloseAsync()
        {
            _hostService.ColorChanged -= OnHostServiceColorChanged;

            await base.CloseAsync();

            _isInitialized = false;
        }

        private void OnHostServiceColorChanged(object sender, ColorEventArgs e)
        {
            _dispatcherService.BeginInvokeIfRequired(() => Color = e.Color);
        }

        private void OnSelectedPluginChanged()
        {
            if (!_isInitialized)
            {
                return;
            }

            _configurationService.SetRoamingValue(ConfigurationKeys.ActivePlugin, SelectedPlugin != null ? SelectedPlugin.FullTypeName : null);
        }
        #endregion
    }
}
