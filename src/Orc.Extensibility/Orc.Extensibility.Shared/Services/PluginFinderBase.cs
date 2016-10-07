﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionsManager.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Catel;
    using Catel.Logging;
    using Catel.Reflection;
    using FileSystem;
    using MethodTimer;

    public abstract class PluginFinderBase : IPluginFinder
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly string[] PluginFileFilters = { "*.dll", "*.exe" };

        // Note: make sure these are lowercase
        private static readonly HashSet<string> KnownAssemblyPrefixesToIgnore = new HashSet<string>(new[]
        {
            "system.",
            "microsoft.",
            "orc.",
            "catel.",
            "orchestra.",
            "fluent.",
            "obsolete",
            "methodtimer",
        });

        private readonly IPluginLocationsProvider _pluginLocationsProvider;
        private readonly IPluginInfoProvider _pluginInfoProvider;
        private readonly IPluginCleanupService _pluginCleanupService;
        private readonly IDirectoryService _directoryService;
        private readonly IFileService _fileService;

        protected PluginFinderBase(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider,
            IPluginCleanupService pluginCleanupService, IDirectoryService directoryService, IFileService fileService)
        {
            Argument.IsNotNull(() => pluginLocationsProvider);
            Argument.IsNotNull(() => pluginInfoProvider);
            Argument.IsNotNull(() => pluginCleanupService);
            Argument.IsNotNull(() => directoryService);
            Argument.IsNotNull(() => fileService);

            _pluginLocationsProvider = pluginLocationsProvider;
            _pluginInfoProvider = pluginInfoProvider;
            _pluginCleanupService = pluginCleanupService;
            _directoryService = directoryService;
            _fileService = fileService;
        }

        [Time]
        public IEnumerable<IPluginInfo> FindPlugins()
        {
            var plugins = new List<IPluginInfo>();

            var appDomain = AppDomain.CurrentDomain;
            appDomain.ReflectionOnlyAssemblyResolve += OnReflectionOnlyAssemblyResolve;

            var pluginDirectories = _pluginLocationsProvider.GetPluginDirectories();

            foreach (var pluginDirectory in pluginDirectories)
            {
                if (!_directoryService.Exists(pluginDirectory))
                {
                    continue;
                }

                // Step 1: top-level plugins (assemblies directly located inside the root)
                foreach (var pluginFileFilter in PluginFileFilters)
                {
                    var rootPlugins = (from file in _directoryService.GetFiles(pluginDirectory, pluginFileFilter)
                                       orderby File.GetLastWriteTime(file) descending
                                       select file).ToList();
                    plugins.AddRange(FindPluginsInAssemblies(rootPlugins.ToArray()));
                }

                // Step 2: treat each subdirectory separately
                var directories = (from directory in _directoryService.GetDirectories(pluginDirectory)
                                   orderby Directory.GetLastWriteTime(directory) descending
                                   select directory).ToList();

                foreach (var directory in directories)
                {
                    plugins.AddRange(FindPluginsInDirectory(directory));
                }
            }

            RemoveDuplicates(plugins);

            appDomain.ReflectionOnlyAssemblyResolve -= OnReflectionOnlyAssemblyResolve;

            return plugins;
        }

        private void RemoveDuplicates(List<IPluginInfo> plugins)
        {
            for (var i = 0; i < plugins.Count; i++)
            {
                var pluginName = plugins[i].FullTypeName;

                var duplicates = (from plugin in plugins
                                  where string.Equals(plugin.FullTypeName, pluginName)
                                  select plugin).ToList();
                if (duplicates.Count > 1)
                {
                    List<IPluginInfo> oldDuplicates = null;

                    // Method 1: use version
                    if (oldDuplicates == null)
                    {
                        try
                        {
                            oldDuplicates = (from duplicate in duplicates
                                             orderby new SemVersion(duplicate.Version) descending
                                             select duplicate).Skip(1).ToList();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to sort versions using Version");
                        }
                    }

                    // Method 2: use last write time
                    if (oldDuplicates == null)
                    {
                        oldDuplicates = (from duplicate in duplicates
                                         orderby File.GetLastWriteTime(duplicate.Location) descending
                                         select duplicate).Skip(1).ToList();
                    }

                    foreach (var oldDuplicate in oldDuplicates)
                    {
                        for (int j = 0; j < plugins.Count; j++)
                        {
                            var pluginInfo = plugins[j];
                            if (pluginInfo.FullTypeName.EqualsIgnoreCase(oldDuplicate.FullTypeName) &&
                                pluginInfo.Version.EqualsIgnoreCase(oldDuplicate.Version))
                            {
                                plugins.RemoveAt(j--);

                                // Stop processing, we must keep at least one
                                break;
                            }
                        }
                    }

                    i = 0;
                }
            }
        }

        [Time]
        protected IEnumerable<IPluginInfo> FindPluginsInDirectory(string pluginDirectory)
        {
            var plugins = new List<IPluginInfo>();

            if (_pluginCleanupService.IsCleanupRequired(pluginDirectory))
            {
                _pluginCleanupService.Cleanup(pluginDirectory);
                return plugins;
            }

            Log.Debug("Searching for plugins in directory '{0}'", pluginDirectory);

            try
            {
                if (!_directoryService.Exists(pluginDirectory))
                {
                    Log.Debug("Directory does not exist, no plugins found");
                    return plugins;
                }

                // Once we are in a good directory, the assembly can be located at any depth
                foreach (var pluginFileFilter in PluginFileFilters)
                {
                    var assemblies = _directoryService.GetFiles(pluginDirectory, pluginFileFilter, SearchOption.AllDirectories);

                    plugins.AddRange(FindPluginsInAssemblies(assemblies));
                }

                Log.Debug("Found '{0}' plugins in directory '{1}'", plugins.Count, pluginDirectory);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to search for plugins in directory '{0}'", pluginDirectory);
            }

            return plugins;
        }

        protected IEnumerable<IPluginInfo> FindPluginsInAssemblies(params string[] assemblyPaths)
        {
            var plugins = new List<IPluginInfo>();

            try
            {
                foreach (var assemblyPath in assemblyPaths)
                {
                    if (ShouldIgnoreAssembly(assemblyPath))
                    {
                        continue;
                    }

                    try
                    {
                        var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
                        plugins.AddRange(FindPluginsInAssembly(assembly));
                    }
                    catch (FileLoadException)
                    {
                        // Ignore, assembly already loaded
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to load assembly '{0}' for reflection, ignoring as possible plugin container", assemblyPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to search for plugins");
            }

            return plugins;
        }

        [Time]
        protected IEnumerable<IPluginInfo> FindPluginsInAssembly(Assembly assembly)
        {
            var plugins = new List<IPluginInfo>();

            Log.Debug("Searching for plugins in assembly '{0}'", assembly.FullName);

            try
            {
                var allTypes = assembly.GetAllTypesSafely();
                var pluginTypes = (from type in allTypes
                                   where !type.IsAbstractEx() && IsPlugin(type)
                                   select type).ToList();

                foreach (var type in pluginTypes)
                {
                    var pluginInfo = _pluginInfoProvider.GetPluginInfo(type);
                    if (pluginInfo != null)
                    {
                        plugins.Add(pluginInfo);
                    }
                }

                Log.Debug("Found '{0}' plugins in assembly '{1}'", plugins.Count, assembly.FullName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to search for plugins in assembly '{0}'", assembly.FullName);
            }

            return plugins;
        }

        private Assembly OnReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs e)
        {
            var requestingAssemblyDirectory = e.RequestingAssembly.GetDirectory();

            var assemblyName = TypeHelper.GetAssemblyNameWithoutOverhead(e.Name);
            var possibleDirectories = new[] { requestingAssemblyDirectory, AssemblyHelper.GetEntryAssembly().GetDirectory() };
            var possibleExtensions = new[] { "dll", "exe" };

            foreach (var possibleDirectory in possibleDirectories)
            {
                foreach (var possibleExtension in possibleExtensions)
                {
                    var expectedPath = Path.Combine(possibleDirectory, $"{assemblyName}.{possibleExtension}");
                    if (_fileService.Exists(expectedPath))
                    {
                        try
                        {
                            var assembly = Assembly.ReflectionOnlyLoadFrom(expectedPath);
                            if (assembly != null)
                            {
                                return assembly;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, $"Failed to load assembly '{expectedPath}' for reflection only");
                        }
                    }
                }
            }

            // As a last result, try to load the assembly automatically
            var autoAssembly = Assembly.ReflectionOnlyLoad(e.Name);
            return autoAssembly;
        }

        protected virtual bool ShouldIgnoreAssembly(string assemblyPath)
        {
            var fileName = Path.GetFileName(assemblyPath).ToLower();

            foreach (var knownAssemblyPrefix in KnownAssemblyPrefixesToIgnore)
            {
                if (fileName.StartsWith(knownAssemblyPrefix))
                {
                    return true;
                }
            }

            if (fileName.Contains(".resources.dll"))
            {
                return true;
            }

            if (fileName.EndsWith(".vshost.exe"))
            {
                return true;
            }

            return false;
        }

        protected abstract bool IsPlugin(Type type);
    }
}