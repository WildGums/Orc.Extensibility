// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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
        private readonly AppDomainRuntimeAssemblyWatcher _appDomainRuntimeAssemblyWatcher;
        private bool _isInitialized;

        public MainViewModel(IHostService hostService, IDispatcherService dispatcherService, IPluginManager pluginManager,
            IConfigurationService configurationService, IRuntimeAssemblyResolverService runtimeAssemblyResolverService,
            AppDomainRuntimeAssemblyWatcher appDomainRuntimeAssemblyWatcher)
        {
            ArgumentNullException.ThrowIfNull(hostService);
            ArgumentNullException.ThrowIfNull(dispatcherService);
            ArgumentNullException.ThrowIfNull(pluginManager);
            ArgumentNullException.ThrowIfNull(configurationService);
            ArgumentNullException.ThrowIfNull(runtimeAssemblyResolverService);
            ArgumentNullException.ThrowIfNull(appDomainRuntimeAssemblyWatcher);

            _hostService = hostService;
            _dispatcherService = dispatcherService;
            _pluginManager = pluginManager;
            _configurationService = configurationService;
            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
            _appDomainRuntimeAssemblyWatcher = appDomainRuntimeAssemblyWatcher;

            RuntimeResolvedAssemblies = new ObservableCollection<IRuntimeAssembly>(appDomainRuntimeAssemblyWatcher.LoadedAssemblies);

            AvailablePlugins = new List<IPluginInfo>();
            RuntimeAssemblies = new List<IRuntimeAssembly>();
            Title = "Orc.Extensibility example";
        }

        public List<IPluginInfo> AvailablePlugins { get; private set; }

        public IPluginInfo? SelectedPlugin { get; set; }

        public List<IRuntimeAssembly> RuntimeAssemblies { get; private set; }

        public ObservableCollection<IRuntimeAssembly> RuntimeResolvedAssemblies { get; private set; }

        public Color Color { get; private set; }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var selectedPlugin = _configurationService.GetRoamingValue(ConfigurationKeys.ActivePlugin, ConfigurationKeys.ActivePluginDefaultValue);
            var plugins = _pluginManager.GetPlugins();

            AvailablePlugins = plugins.ToList();
            SelectedPlugin = (from plugin in AvailablePlugins
                              where plugin.FullTypeName.Contains(selectedPlugin)
                              select plugin).FirstOrDefault();

            _hostService.ColorChanged += OnHostServiceColorChanged;
            _appDomainRuntimeAssemblyWatcher.AssemblyLoaded += OnRuntimeAssemblyWatcherAssemblyLoaded;

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
            _appDomainRuntimeAssemblyWatcher.AssemblyLoaded -= OnRuntimeAssemblyWatcherAssemblyLoaded;

            await base.CloseAsync();

            _isInitialized = false;
        }

        private void OnHostServiceColorChanged(object? sender, ColorEventArgs e)
        {
            _dispatcherService.BeginInvokeIfRequired(() => Color = e.Color);
        }

        private void OnRuntimeAssemblyWatcherAssemblyLoaded(object? sender, RuntimeLoadedAssemblyEventArgs e)
        {
            _dispatcherService.BeginInvokeIfRequired(() => RuntimeResolvedAssemblies.Add(e.ResolvedRuntimeAssembly));
        }

        private void OnSelectedPluginChanged()
        {
            if (!_isInitialized)
            {
                return;
            }

            _configurationService.SetRoamingValue(ConfigurationKeys.ActivePlugin, SelectedPlugin is not null ? SelectedPlugin.FullTypeName : null);
        }
    }
}
