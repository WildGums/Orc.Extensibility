// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginFactory.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Orc.Extensibility
{
    using System;
    using System.Reflection;
    using Catel;
    using Catel.IoC;
    using Catel.Logging;
    using Catel.Reflection;
    using MethodTimer;

    public class PluginFactory : IPluginFactory
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly ITypeFactory _typeFactory;

        public PluginFactory(ITypeFactory typeFactory)
        {
            Argument.IsNotNull(() => typeFactory);

            _typeFactory = typeFactory;
        }

        [Time]
        public object CreatePlugin(IPluginInfo pluginInfo)
        {
            Argument.IsNotNull(() => pluginInfo);

            try
            {
                Log.Debug($"Creating plugin '{pluginInfo}'");

                Log.Debug($"  1. Loading assembly from '{pluginInfo.Location}'");

                // Note: we must use LoadFrom instead of Load
                var assembly = Assembly.LoadFrom(pluginInfo.Location);

                Log.Debug($"  2. Getting type '{pluginInfo.FullTypeName}' from loaded assembly");

                var type = assembly.GetType(pluginInfo.FullTypeName);

                Log.Debug($"  3. Instantiating type '{type.GetSafeFullName(true)}'");

                var plugin = _typeFactory.CreateInstance(type);

                Log.Debug($"Plugin creation resulted in an instance: '{plugin != null}'");

                // Workaround for loading assemblies
                TypeCache.InitializeTypes(type.GetAssemblyEx(), true);

                return plugin;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to create plugin '{pluginInfo}'");

                throw;
            }
        }
    }
}
