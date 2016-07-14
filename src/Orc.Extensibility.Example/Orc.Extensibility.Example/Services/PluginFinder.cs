// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginFinder.cs" company="WildGums">
//   Copyright (c) 2008 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility.Example.Services
{
    using System;
    using System.Linq;
    using Catel;
    using Catel.Reflection;

    public class PluginFinder : Orc.Extensibility.PluginFinderBase
    {
        private readonly string _pluginName = typeof(ICustomPlugin).Name;

        public PluginFinder(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider,
            IPluginCleanupService pluginCleanupService)
            : base(pluginLocationsProvider, pluginInfoProvider, pluginCleanupService)
        {
        }

        protected override bool IsPlugin(Type type)
        {
            // Note: since we are in a reflection-only context here, you can't compare actual types, but need to use string names
            return (from iface in type.GetInterfacesEx()
                    where iface.Name.Equals(_pluginName)
                    select iface).Any();
        }

        protected override bool ShouldIgnoreAssembly(string assemblyPath)
        {
            // Since by default, the plugin finder ignores Orc.* assemblies, we need to override it here
            if (assemblyPath.ContainsIgnoreCase("Orc.Extensibility.Example"))
            {
                return false;
            }

            return base.ShouldIgnoreAssembly(assemblyPath);
        }
    }
}