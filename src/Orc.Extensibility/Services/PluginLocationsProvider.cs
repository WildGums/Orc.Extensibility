namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.IO;
using Catel;
using Catel.IO;
using Catel.Reflection;
using Catel.Services;

public class PluginLocationsProvider : IPluginLocationsProvider
{
    private readonly IAppDataService _appDataService;

    public PluginLocationsProvider(IAppDataService appDataService)
    {
        ArgumentNullException.ThrowIfNull(appDataService);

        _appDataService = appDataService;
    }

    public virtual IEnumerable<string> GetPluginDirectories()
    {
        var directories = new List<string>();

        var pluginsDirectory = System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(ApplicationDataTarget.UserRoaming), "plugins");
        if (ValidateDirectory(pluginsDirectory))
        {
            directories.Add(pluginsDirectory);
        }

        var currentDirectory = Environment.CurrentDirectory;
        if (ValidateDirectory(currentDirectory))
        {
            directories.Add(currentDirectory);
        }

        var appDirectory = AssemblyHelper.GetRequiredEntryAssembly().GetDirectory();
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