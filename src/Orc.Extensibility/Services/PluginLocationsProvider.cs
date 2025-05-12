namespace Orc.Extensibility;

using System;
using System.Collections.Generic;
using System.Linq;
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

    [ObsoleteEx(ReplacementTypeOrMember = nameof(GetPluginLocations), TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
    public virtual IReadOnlyList<string> GetPluginDirectories()
    {
        var directories = new List<string>();

        var pluginsDirectory = System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(ApplicationDataTarget.UserRoaming), "plugins");
        if (ValidateDirectory(pluginsDirectory) &&
            !directories.Any(x => x.EqualsIgnoreCase(pluginsDirectory)))
        {
            directories.Add(pluginsDirectory);
        }

        var currentDirectory = Environment.CurrentDirectory;
        if (ValidateDirectory(currentDirectory) &&
            !directories.Any(x => x.EqualsIgnoreCase(currentDirectory)))
        {
            directories.Add(currentDirectory);
        }

        var appDirectory = AssemblyHelper.GetRequiredEntryAssembly().GetDirectory();
        if (ValidateDirectory(appDirectory) &&
            !directories.Any(x => x.EqualsIgnoreCase(appDirectory)))
        {
            directories.Add(appDirectory);
        }

        return directories;
    }

    public virtual IReadOnlyList<PluginProbingLocation> GetPluginLocations()
    {
        var directories = new List<PluginProbingLocation>();

        var pluginsDirectory = System.IO.Path.Combine(_appDataService.GetApplicationDataDirectory(ApplicationDataTarget.UserRoaming), "plugins");
        if (ValidateDirectory(pluginsDirectory) &&
            !directories.Any(x => x.Location.EqualsIgnoreCase(pluginsDirectory)))
        {
            // Note: this is the default, and should be added as recursive since
            // chances are very high that the user will add plugins in subfolders
            directories.Add(new PluginProbingLocation
            {
                Location = pluginsDirectory,
                IsRecursive = true
            });
        }

        var currentDirectory = Environment.CurrentDirectory;
        if (ValidateDirectory(currentDirectory) &&
            !directories.Any(x => x.Location.EqualsIgnoreCase(currentDirectory)))
        {
            // Note: don't add as recursive, if there is a "plugins" subfolder,
            // it should be added manually for better performance
            directories.Add(new PluginProbingLocation
            {
                Location = currentDirectory,
                IsRecursive = false
            });
        }

        var appDirectory = AssemblyHelper.GetRequiredEntryAssembly().GetDirectory();
        if (ValidateDirectory(appDirectory) &&
            !directories.Any(x => x.Location.EqualsIgnoreCase(appDirectory)))
        {
            // Note: don't add as recursive, if there is a "plugins" subfolder,
            // it should be added manually for better performance
            directories.Add(new PluginProbingLocation
            {
                Location = appDirectory,
                IsRecursive = false
            });
        }

        // Ensure backwards compatibility with the old method name, but we add it at the end
        // so that the new method is preferred over the old one
        var oldPluginDirectories = GetPluginDirectories();

        foreach (var oldPluginDirectory in oldPluginDirectories)
        {
            if (ValidateDirectory(oldPluginDirectory) &&
                !directories.Any(x => x.Location.EqualsIgnoreCase(oldPluginDirectory)))
            {
                // Note: for backwards compatibility, we add the old plugin directory as recursive
                directories.Add(new PluginProbingLocation
                {
                    Location = oldPluginDirectory,
                    IsRecursive = true
                });
            }
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
