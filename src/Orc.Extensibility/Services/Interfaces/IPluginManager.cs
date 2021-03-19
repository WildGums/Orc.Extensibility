// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPluginManager.cs" company="WildGums">
//   Copyright (c) 2012 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPluginManager
    {
        IEnumerable<IPluginInfo> GetPlugins();
        Task RefreshAsync();
    }
}
