﻿namespace Orc.Extensibility;

using System;
using System.Linq;
using Catel.Logging;
using FileSystem;

public class PluginCleanupService : IPluginCleanupService
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private const string DeleteMeFilter = "*.deleteme";

    private readonly IFileService _fileService;
    private readonly IDirectoryService _directoryService;

    public PluginCleanupService(IFileService fileService, IDirectoryService directoryService)
    {
        ArgumentNullException.ThrowIfNull(fileService);
        ArgumentNullException.ThrowIfNull(directoryService);

        _fileService = fileService;
        _directoryService = directoryService;
    }

    public bool IsCleanupRequired(string directory)
    {
        var deleteMeFile = GetDeleteMeFile(directory);
        return !string.IsNullOrWhiteSpace(deleteMeFile);
    }

    public void Cleanup(string directory)
    {
        Log.Debug("Cleaning up plugin at '{0}'", directory);

        if (!IsCleanupRequired(directory))
        {
            Log.Debug("Cleaning up of plugin is not required");
            return;
        }

        var deleteMeFile = GetDeleteMeFile(directory);
        if (deleteMeFile is null)
        {
            return;
        }

        var succeeded = false;

        try
        {
            _directoryService.Delete(directory, true);
            succeeded = !_directoryService.Exists(directory);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to clean up plugin at '{0}'", directory);
        }

        if (succeeded)
        {
            return;
        }

        try
        {
            // Create the file again, we didn't delete successfully
            if (_fileService.Exists(deleteMeFile))
            {
                return;
            }

            using (_fileService.Create(deleteMeFile))
            {
                // No need to write contents
            }
        }
        catch (Exception)
        {
            // Just ignore
        }
    }

    private string? GetDeleteMeFile(string directory)
    {
        try
        {
            var files = _directoryService.GetFiles(directory, DeleteMeFilter);
            return files.FirstOrDefault();
        }
        catch (Exception)
        {
            // Just ignore
        }

        return null;
    }
}
