// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ILoadedPluginService.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;

    public interface ILoadedPluginService
    {
        #region Events
        event EventHandler<PluginEventArgs> PluginLoaded;
        #endregion

        #region Methods
        List<IPluginInfo> GetLoadedPlugins();

        void AddPlugin(IPluginInfo pluginInfo);
        #endregion
    }
}
