// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExtensionsManager.cs" company="WildGums">
//   Copyright (c) 2008 - 2015 WildGums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
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
            "google",
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
            "ozcode.",
            "penimc",
            "presentation", // should ignore PresentationFramework, PresentationNative, etc
            "protobuf-net",
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
        public async Task<IEnumerable<IPluginInfo>> FindPluginsAsync()
        {
            var pluginProbingContext = new PluginProbingContext();

            // Step 1: Check plugins already in the current AppDomain
            await FindPluginsInLoadedAssembliesAsync(pluginProbingContext);

            // Step 2: Check for plugins outside AppDomain
            await FindPluginsInUnloadedAssembliesAsync(pluginProbingContext);

            await RemoveDuplicatesAsync(pluginProbingContext);

            return pluginProbingContext.Plugins;
        }

        [Time]
        protected virtual async Task FindPluginsInLoadedAssembliesAsync(PluginProbingContext context)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (CanInvestigateAssembly(context, assembly))
                {
                    await FindPluginsInAssemblyAsync(context, assembly);
                }
            }
        }

        [Time]
        protected virtual async Task FindPluginsInUnloadedAssembliesAsync(PluginProbingContext context)
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

                    await FindPluginsInAssembliesAsync(context, potentialPluginFiles.ToArray());
                }

                // Step 2: treat each subdirectory separately
                var directories = (from directory in _directoryService.GetDirectories(pluginDirectory)
                                   orderby Directory.GetLastWriteTime(directory) descending
                                   select directory).ToList();

                foreach (var directory in directories)
                {
                    await FindPluginsInDirectoryAsync(context, directory);
                }
            }
        }

        protected virtual async Task RemoveDuplicatesAsync(PluginProbingContext context)
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

                            await _runtimeAssemblyResolverService.UnregisterAssemblyAsync(oldDuplicateLocation);
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

        [Time("{pluginDirectory}")]
        protected virtual async Task FindPluginsInDirectoryAsync(PluginProbingContext context, string pluginDirectory)
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

                    await FindPluginsInAssembliesAsync(context, assemblies);
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

        protected async Task FindPluginsInAssembliesAsync(PluginProbingContext context, params string[] assemblyPaths)
        {
            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    if (CanInvestigateAssembly(context, assemblyPath))
                    {
                        await FindPluginsInAssemblyAsync(context, assemblyPath);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to search for plugins");
                }
            }
        }

        [Time("{assemblyPath}")]
        protected virtual async Task FindPluginsInAssemblyAsync(PluginProbingContext context, string assemblyPath)
        {
            // Double check that this is a resolvable app (exe don't seem to be resolvable)
            var isPeAssembly = _assemblyReflectionService.IsPeAssembly(assemblyPath);
            if (!isPeAssembly)
            {
                return;
            }

            // Register assembly in their own plugin load context
            await _runtimeAssemblyResolverService.RegisterAssemblyAsync(assemblyPath);

            // Important: all types already in the app domain should be included as well
            var resolvableAssemblyPaths = new List<string>(new string[] { assemblyPath });

            resolvableAssemblyPaths.AddRange(FindResolvableAssemblyPaths(assemblyPath));

            var resolver = new PathAssemblyResolver(resolvableAssemblyPaths);

            using (var metadataLoadContext = new MetadataLoadContext(resolver))
            {
                var assembly = metadataLoadContext.LoadFromAssemblyPath(assemblyPath);
                await FindPluginsInAssemblyAsync(context, assembly);
            }
        }

        protected virtual async Task FindPluginsInAssemblyAsync(PluginProbingContext context, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                try
                {
                    // Try fastest ways out first, the sooner we exit, the better

                    // ************************************************************************************
                    // NON-FIRST CHANCE EXCEPTION THROWING CHECKS
                    // ************************************************************************************

                    if (type.IsInterfaceEx())
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

                    if (!IsPluginFastPreCheck(context, type))
                    {
                        continue;
                    }

                    // ************************************************************************************
                    // FIRST CHANCE EXCEPTION THROWING CHECKS
                    // ************************************************************************************

                    if (!type.IsClassEx())
                    {
                        continue;
                    }

                    if (IsPlugin(context, type))
                    {
                        var pluginInfo = _pluginInfoProvider.GetPluginInfo(assembly.Location, type);
                        if (pluginInfo is not null)
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
            var assemblyVersions = new Dictionary<string, Version>(StringComparer.OrdinalIgnoreCase);

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

                    var fileName = Path.GetFileNameWithoutExtension(location);
                    var version = GetFileVersion(location);

                    assemblyVersions[fileName] = version;
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
                    if (!_fileService.Exists(runtimeAssembly.Location))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(runtimeAssembly.Location);
                    var version = GetFileVersion(runtimeAssembly.Location);

                    if (assemblyVersions.TryGetValue(fileName, out var existingVersion))
                    {
                        if (existingVersion != version)
                        {
                            // Important: just log, but still add the path
                            Log.Warning($"Already loaded '{fileName}' version '{existingVersion}', but also found runtime assembly '{version}'. The already loaded assembly will be used to investigate '{assemblyPath}'");
                        }
                    }
                    else
                    {
                        assemblyVersions[fileName] = version;
                    }

                    paths.Add(runtimeAssembly.Location);
                }
            }

            return paths;
        }

        protected virtual Version GetFileVersion(string fileName)
        {
            try
            {
                return AssemblyName.GetAssemblyName(fileName)?.Version ?? new Version("0.0.0");
            }
            catch (Exception)
            {
                try
                {
                    // Fall back to file version
                    var fileVersionInfo = FileVersionInfo.GetVersionInfo(fileName);
                    var fileVersion = fileVersionInfo?.FileVersion ?? "0.0.0";
                    return new Version(fileVersion);
                }
                catch (Exception)
                {
                    // Aware of Argument Exception and other possible
                    return new Version("0.0.0");
                }
            }
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

            if (fileName.ContainsIgnoreCase(".resources.dll"))
            {
                return true;
            }

            if (fileName.ContainsIgnoreCase("update.exe"))
            {
                return true;
            }

            if (fileName.EndsWithIgnoreCase(".vshost.exe"))
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

        /// <summary>
        /// Very fast way of checking whether a type should be checked for plugin materials.
        /// <para />
        /// This method should return as fast as possible. This code will be executed before any actual type
        /// checking takes place and could prevent first-chance exceptions when checking specific type elements
        /// such as <c>IsClass</c> or <c>IsValueType</c>.
        /// </summary>
        /// <param name="context">The plugin probing context.</param>
        /// <param name="type">The type to investigate.</param>
        /// <returns><c>true</c> if the code should continue checking for plugin materials for this type; otherwise, <c>false</c>.</returns>
        protected virtual bool IsPluginFastPreCheck(PluginProbingContext context, Type type)
        {
            return true;
        }

        protected abstract bool IsPlugin(PluginProbingContext context, Type type);
    }
}
