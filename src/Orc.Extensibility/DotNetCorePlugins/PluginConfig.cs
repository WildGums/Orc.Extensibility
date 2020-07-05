// Note: comes from https://github.com/natemcmaster/DotNetCorePlugins/

#if NETCORE

namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using Catel;
    using Catel.Logging;

    internal class PluginConfig
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Initializes a new instance of <see cref="PluginConfig" />
        /// </summary>
        /// <param name="mainAssemblyPath">The full file path to the main assembly for the plugin.</param>
        public PluginConfig(string mainAssemblyPath)
        {
            Argument.IsNotNull(() => mainAssemblyPath);

            if (!Path.IsPathRooted(mainAssemblyPath))
            {
                throw Log.ErrorAndCreateException<InvalidOperationException>("Value must be an absolute file path");
            }

            MainAssemblyPath = mainAssemblyPath;
        }

        public string MainAssemblyPath { get; }

        public ICollection<AssemblyName> PrivateAssemblies { get; protected set; } = new List<AssemblyName>();

        public ICollection<AssemblyName> SharedAssemblies { get; protected set; } = new List<AssemblyName>();

        public List<string> AdditionalProbingPaths { get; protected set; } = new List<string>();

        public bool PreferSharedTypes { get; set; }

        public AssemblyLoadContext DefaultContext { get; set; } = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()) ?? AssemblyLoadContext.Default;
    }
}

#endif
