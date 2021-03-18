﻿namespace Orc.Extensibility
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPluginFinder
    {
        Task<IEnumerable<IPluginInfo>> FindPluginsAsync();
    }
}
