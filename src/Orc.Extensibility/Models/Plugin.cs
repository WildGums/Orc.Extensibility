namespace Orc.Extensibility
{
    using System;

    public class Plugin : IPlugin
    {
        public Plugin(object instance, IPluginInfo info)
        {
            ArgumentNullException.ThrowIfNull(instance);
            ArgumentNullException.ThrowIfNull(info);

            Instance = instance;
            Info = info;
        }

        public object Instance { get; private set; }

        public IPluginInfo Info { get; private set; }

        public override string ToString()
        {
            return $"{Info}";
        }
    }
}
