// --------------------------------------------------------------------------------------------------------------------
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
    using System.Security.Cryptography.X509Certificates;
    using Catel;
    using Catel.Logging;
    using Catel.Reflection;
    using FileSystem;
    using MethodTimer;

    public abstract class PluginFinderBase : IPluginFinder
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private static readonly string[] PluginFileFilters = { "*.dll", "*.exe" };

        // Note: make sure these are lowercase & alphabetically sorted
        private static readonly HashSet<string> KnownAssemblyPrefixesToIgnore = new HashSet<string>(new[]
        {
            "accessibility",
            "api-ms",
            "catel.",
            "clr", // should ignore clretwrc, clrcor, clrcompression, clrjit, etc
            "costura.",
            "controlzex",
            "coreclr",
            "dbgshim",
            "d3dcompiler",
            "directwriteforwarder",
            "dotnetzip",
            "fluent.",
            "host", // should ignore hostpolicy, hostfxr, etc
            "libskiasharp",
            "ionic.zip.",
            "mahapps.",
            "methodtimer.",
            "microsoft.",
            "moduleinit.",
            "mscor", // should ignore mscorrc, mscordbi, mscordaccore, etc
            "mono.cecil.",
            "mscorlib",
	        "netstandard",
			"newtonsoft.",
            "nuget.",
            "obsolete.",
            "orc.",
            "orchestra.",
			"penimc",
            "presentation", // should ignore PresentationFramework, PresentationNative, etc
            "reachframework",
            "skiasharp",
            "system.",
            "ucrtbase",
            "uiautomation", // should ignore multiple times
			"unins0",
            "vcruntime140",
            "windowsbase",
            "windowsformsintegration",
            "wpfgfx"
        });

        private readonly IPluginLocationsProvider _pluginLocationsProvider;
        private readonly IPluginInfoProvider _pluginInfoProvider;
        private readonly IPluginCleanupService _pluginCleanupService;
        private readonly IDirectoryService _directoryService;
        private readonly IFileService _fileService;
        private readonly IAssemblyReflectionService _assemblyReflectionService;
        private readonly IRuntimeAssemblyResolverService _runtimeAssemblyResolverService;
        private readonly List<string> _appDomainResolvablePaths = new List<string>();

        protected PluginFinderBase(IPluginLocationsProvider pluginLocationsProvider, IPluginInfoProvider pluginInfoProvider,
            IPluginCleanupService pluginCleanupService, IDirectoryService directoryService, IFileService fileService,
            IAssemblyReflectionService assemblyReflectionService, IRuntimeAssemblyResolverService runtimeAssemblyResolverService)
        {
            Argument.IsNotNull(() => pluginLocationsProvider);
            Argument.IsNotNull(() => pluginInfoProvider);
            Argument.IsNotNull(() => pluginCleanupService);
            Argument.IsNotNull(() => directoryService);
            Argument.IsNotNull(() => fileService);
            Argument.IsNotNull(() => assemblyReflectionService);
            Argument.IsNotNull(() => runtimeAssemblyResolverService);

            _pluginLocationsProvider = pluginLocationsProvider;
            _pluginInfoProvider = pluginInfoProvider;
            _pluginCleanupService = pluginCleanupService;
            _directoryService = directoryService;
            _fileService = fileService;
            _assemblyReflectionService = assemblyReflectionService;
            _runtimeAssemblyResolverService = runtimeAssemblyResolverService;
        }

        [Time]
        public IEnumerable<IPluginInfo> FindPlugins()
        {
            var pluginProbingContext = new PluginProbingContext();

            // Step 1: Check plugins already in the current AppDomain
            FindPluginsInLoadedAssemblies(pluginProbingContext);

            // Step 2: Check for plugins outside AppDomain
            FindPluginsInUnloadedAssemblies(pluginProbingContext);

            RemoveDuplicates(pluginProbingContext);

            return pluginProbingContext.Plugins;
        }

        [Time]
        protected virtual void FindPluginsInLoadedAssemblies(PluginProbingContext context)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (CanInvestigateAssembly(context, assembly))
                {
                    FindPluginsInAssembly(context, assembly);
                }
            }
        }

        [Time]
        protected virtual void FindPluginsInUnloadedAssemblies(PluginProbingContext context)
        {
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
                    var potentialPluginFiles = (from file in _directoryService.GetFiles(pluginDirectory, pluginFileFilter)
                                                orderby File.GetLastWriteTime(file) descending
                                                select file).ToList();

                    FindPluginsInAssemblies(context, potentialPluginFiles.ToArray());
                }

                // Step 2: treat each subdirectory separately
                var directories = (from directory in _directoryService.GetDirectories(pluginDirectory)
                                   orderby Directory.GetLastWriteTime(directory) descending
                                   select directory).ToList();

                foreach (var directory in directories)
                {
                    FindPluginsInDirectory(context, directory);
                }
            }
        }

        protected virtual void RemoveDuplicates(PluginProbingContext context)
        {
            for (var i = 0; i < context.Plugins.Count; i++)
            {
                var pluginName = context.Plugins[i].FullTypeName;

                var duplicates = (from plugin in context.Plugins
                                  where string.Equals(plugin.FullTypeName, pluginName)
                                  select plugin).ToList();
                if (duplicates.Count > 1)
                {
                    // Find the older ones so we keep 1
                    var oldDuplicates = GetOldestDuplicates(duplicates);

                    foreach (var oldDuplicate in oldDuplicates)
                    {
                        var oldDuplicateLocation = oldDuplicate.Location;

                        for (var j = 0; j < context.Plugins.Count; j++)
                        {
                            var pluginInfo = context.Plugins[j];

                            // Version must always match
                            if (!pluginInfo.Version.EqualsIgnoreCase(oldDuplicate.Version))
                            {
                                continue;
                            }

                            // Type must always match
                            if (!pluginInfo.FullTypeName.EqualsIgnoreCase(oldDuplicate.FullTypeName))
                            {
                                continue;
                            }

                            // If we have a location, location must match
                            if (!string.IsNullOrWhiteSpace(oldDuplicateLocation))
                            {
                                if (!pluginInfo.Location.EqualsIgnoreCase(oldDuplicate.Location))
                                {
                                    continue;
                                }
                            }

                            context.Plugins.RemoveAt(j);
                        }
                    }

                    // Reset
                    i = 0;
                }
            }
        }

        protected virtual List<IPluginInfo> GetOldestDuplicates(List<IPluginInfo> duplicates)
        {
            List<IPluginInfo> oldDuplicates = null;

            // Method 1: use version
            if (oldDuplicates is null)
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
            if (oldDuplicates is null)
            {
                oldDuplicates = (from duplicate in duplicates
                                 orderby File.GetLastWriteTime(duplicate.Location) descending
                                 select duplicate).Skip(1).ToList();
            }

            return oldDuplicates;
        }

        [Time]
        protected virtual void FindPluginsInDirectory(PluginProbingContext context, string pluginDirectory)
        {
            if (_pluginCleanupService.IsCleanupRequired(pluginDirectory))
            {
                _pluginCleanupService.Cleanup(pluginDirectory);
                return;
            }

            Log.Debug("Searching for plugins in directory '{0}'", pluginDirectory);

            try
            {
                if (!_directoryService.Exists(pluginDirectory))
                {
                    Log.Debug("Directory does not exist, no plugins found");
                    return;
                }

                // Once we are in a good directory, the assembly can be located at any depth
                foreach (var pluginFileFilter in PluginFileFilters)
                {
                    var assemblies = _directoryService.GetFiles(pluginDirectory, pluginFileFilter, SearchOption.AllDirectories);

                    FindPluginsInAssemblies(context, assemblies);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to search for plugins in directory '{0}'", pluginDirectory);
            }
        }

        protected virtual bool CanInvestigateAssembly(PluginProbingContext context, Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return false;
            }

            return CanInvestigateAssembly(context, assembly.Location);
        }

        protected virtual bool CanInvestigateAssembly(PluginProbingContext context, string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                return false;
            }

            if (context.Locations.Contains(assemblyPath))
            {
                return false;
            }

            context.Locations.Add(assemblyPath);

            if (ShouldIgnoreAssembly(assemblyPath))
            {
                return false;
            }

            return true;
        }

        protected void FindPluginsInAssemblies(PluginProbingContext context, params string[] assemblyPaths)
        {
            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    FindPluginsInAssembly(context, assemblyPath);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to search for plugins");
                }
            }
        }

        [Time("{assemblyPath}")]
        protected virtual void FindPluginsInAssembly(PluginProbingContext context, string assemblyPath)
        {
            if (!CanInvestigateAssembly(context, assemblyPath))
            {
                return;
            }

            // Double check that this is a resolvable app (exe don't seem to be resolvable)
            var isPeAssembly = _assemblyReflectionService.IsPeAssembly(assemblyPath);
            if (!isPeAssembly)
            {
                return;
            }

            _runtimeAssemblyResolverService.RegisterAssembly(assemblyPath);

            // Important: all types already in the app domain should be included as well
            var resolvableAssemblyPaths = new List<string>(new string[] { assemblyPath });

            resolvableAssemblyPaths.AddRange(FindResolvableAssemblyPaths(assemblyPath));

            var resolver = new PathAssemblyResolver(resolvableAssemblyPaths);

            using (var metadataLoadContext = new MetadataLoadContext(resolver))
            {
                var assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
                FindPluginsInAssembly(context, assembly);
            }
        }

        protected virtual void FindPluginsInAssembly(PluginProbingContext context, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                try
                {
                    if (!type.IsClassEx())
                    {
                        continue;
                    }

                    if (type.IsAbstractEx())
                    {
                        continue;
                    }

                    // Don't support nested private classes as plugins. Developers should
                    // expose their plugins, not nest them as private. This saves a lot of 
                    // generated classes to investigate. If someone really need this, they can
                    // override this method and implement their own
                    if (type.IsNestedPrivate)
                    {
                        continue;
                    }

                    // ignore compiler specific classes (such as <>c_displayclass, etc)
                    if (type.Name.Contains("<>c"))
                    {
                        continue;
                    }

                    if (IsPlugin(context, type))
                    {
                        var pluginInfo = _pluginInfoProvider.GetPluginInfo(assembly.Location, type);
                        if (pluginInfo != null)
                        {
                            Log.Debug($"Found plugin '{pluginInfo}' in assembly '{assembly.Location}'");

                            context.Plugins.Add(pluginInfo);
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore
                }
            }
        }

        protected virtual List<string> FindResolvableAssemblyPaths(string assemblyPath)
        {
            if (_appDomainResolvablePaths.Count == 0)
            {
                foreach (var loadedAssembly in AppDomain.CurrentDomain.GetLoadedAssemblies())
                {
                    if (loadedAssembly.IsDynamic)
                    {
                        continue;
                    }

                    var location = loadedAssembly.Location;
                    if (string.IsNullOrWhiteSpace(location) || !_fileService.Exists(location))
                    {
                        continue;
                    }

                    var isPeAssembly = _assemblyReflectionService.IsPeAssembly(location);
                    if (!isPeAssembly)
                    {
                        continue;
                    }

                    _appDomainResolvablePaths.Add(location);
                }
            }

            var paths = new List<string>(_appDomainResolvablePaths);

            // Always add runtime assemblies, they could be changed. We could (should?) maybe do this *per assembly*
            var pluginLoadContext = (from x in _runtimeAssemblyResolverService.GetPluginLoadContexts()
                                     where x.PluginLocation.EqualsIgnoreCase(assemblyPath)
                                     select x).FirstOrDefault();
            if (pluginLoadContext is not null)
            {
                foreach (var runtimeAssembly in pluginLoadContext.RuntimeAssemblies)
                {
                    paths.Add(runtimeAssembly.Location);
                }
            }

            return paths;
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

        /// <summary>
        /// Checks whether the specified file is signed.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <param name="subjectName">If not <c>null</c>, the certificate must match this subject name.</param>
        /// <returns></returns>
        protected virtual bool IsSigned(string fileName, string subjectName = null)
        {
            try
            {
                var certificate = X509Certificate.CreateFromSignedFile(fileName);

                if (!string.IsNullOrWhiteSpace(subjectName))
                {
                    if (!certificate.Subject.ContainsIgnoreCase(subjectName))
                    {
                        Log.Debug($"File '{fileName}' is signed with subject name '{certificate.Subject}', not matching the requested one so not allowing loading of assembly");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, $"File '{fileName}' is not signed, not allowing loading of assembly");
                return false;
            }
        }

        protected abstract bool IsPlugin(PluginProbingContext context, Type type);
    }
}
