// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PluginLocationsProvider.cs" company="WildGums">
//   Copyright (c) 2012 - 2016 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using Catel;
    using Catel.IO;
    using Catel.Reflection;

    public class PluginLocationsProvider : IPluginLocationsProvider
    {
        public virtual IEnumerable<string> GetPluginDirectories()
        {
            var directories = new List<string>();

            var pluginsDirectory = Path.Combine(Path.GetApplicationDataDirectory(), "plugins");
            if (ValidateDirectory(pluginsDirectory))
            {
                directories.Add(pluginsDirectory);
            }

            var currentDirectory = Environment.CurrentDirectory;
            if (ValidateDirectory(currentDirectory))
            {
                directories.Add(currentDirectory);
            }

            var appDirectory = AssemblyHelper.GetEntryAssembly().GetDirectory();
            if (ValidateDirectory(appDirectory))
            {
                directories.Add(appDirectory);
            }

            return directories;
        }

        protected virtual bool ValidateDirectory(string directory)
        {
            if (directory is null)
            {
                return false;
            }

            // We never ever want to include system directory
            if (directory.ContainsIgnoreCase("\\windows\\"))
            {
                return false;
            }

            return true;
        }
    }
}
