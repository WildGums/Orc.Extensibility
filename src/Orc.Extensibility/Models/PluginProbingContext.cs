namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;

    public class PluginProbingContext
    {
        public PluginProbingContext()
        {
            Plugins = new List<IPluginInfo>();
            Locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public List<IPluginInfo> Plugins { get; private set; }

        public HashSet<string> Locations { get; private set; }
    }
}
