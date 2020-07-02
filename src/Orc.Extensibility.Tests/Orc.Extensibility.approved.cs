﻿[assembly: System.Resources.NeutralResourcesLanguage("en-US")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v3.1", FrameworkDisplayName="")]
public static class ModuleInitializer
{
    public static void Initialize() { }
}
namespace Orc.Extensibility
{
    public class AssemblyReflectionService : Orc.Extensibility.IAssemblyReflectionService
    {
        public AssemblyReflectionService(Orc.FileSystem.IFileService fileService) { }
        public virtual bool IsPeAssembly(string assemblyPath) { }
    }
    public static class CustomAttributeDataExtensions
    {
        public static object GetAttributeValue<TAttribute>(this System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> customAttributes)
            where TAttribute : System.Attribute { }
        public static System.Collections.Generic.List<object> GetAttributeValues<TAttribute>(this System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> customAttributes)
            where TAttribute : System.Attribute { }
    }
    public interface IAssemblyReflectionService
    {
        bool IsPeAssembly(string assemblyPath);
    }
    public interface ILoadedPluginService
    {
        event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        void AddPlugin(Orc.Extensibility.IPluginInfo pluginInfo);
        System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> GetLoadedPlugins();
    }
    public interface IMultiplePluginsService : Orc.Extensibility.IPluginService
    {
        System.Collections.Generic.IEnumerable<Orc.Extensibility.IPlugin> ConfigureAndLoadPlugins(params string[] requestedPlugins);
    }
    public interface IPlugin
    {
        Orc.Extensibility.IPluginInfo Info { get; }
        object Instance { get; }
    }
    public interface IPluginCleanupService
    {
        void Cleanup(string directory);
        bool IsCleanupRequired(string directory);
    }
    public interface IPluginConfiguration
    {
        string FeedName { get; set; }
        string FeedUrl { get; set; }
        string PackagesDirectory { get; set; }
    }
    public interface IPluginFactory
    {
        object CreatePlugin(Orc.Extensibility.IPluginInfo pluginInfo);
    }
    public interface IPluginFinder
    {
        System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> FindPlugins();
    }
    public static class IPluginFinderExtensions { }
    public interface IPluginInfo
    {
        System.Collections.Generic.List<string> Aliases { get; }
        string AssemblyName { get; }
        string Company { get; set; }
        string Customer { get; set; }
        string Description { get; set; }
        string FullTypeName { get; }
        string Location { get; }
        string Name { get; set; }
        string Version { get; set; }
    }
    public interface IPluginInfoProvider
    {
        Orc.Extensibility.IPluginInfo GetPluginInfo(string location, System.Type type);
    }
    public interface IPluginLocationsProvider
    {
        System.Collections.Generic.IEnumerable<string> GetPluginDirectories();
    }
    public interface IPluginManager
    {
        System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> GetPlugins(bool forceRefresh = false);
        void Refresh();
    }
    public static class IPluginManagerExtensions { }
    public interface IPluginService
    {
        event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
    }
    public interface ISinglePluginService : Orc.Extensibility.IPluginService
    {
        Orc.Extensibility.IPlugin ConfigureAndLoadPlugin(string expectedPlugin, string defaultPlugin);
    }
    public class LoadedPluginService : Orc.Extensibility.ILoadedPluginService
    {
        public LoadedPluginService() { }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public void AddPlugin(Orc.Extensibility.IPluginInfo pluginInfo) { }
        public System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> GetLoadedPlugins() { }
    }
    public class MultiplePluginsService : Orc.Extensibility.IMultiplePluginsService, Orc.Extensibility.IPluginService
    {
        public MultiplePluginsService(Orc.Extensibility.IPluginManager pluginManager, Orc.Extensibility.IPluginFactory pluginFactory, Orc.Extensibility.ILoadedPluginService loadedPluginService) { }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
        protected virtual Orc.Extensibility.Plugin ConfigureAndLoadPlugin(Orc.Extensibility.IPluginInfo pluginToLoad, bool isLastTry) { }
        public virtual System.Collections.Generic.IEnumerable<Orc.Extensibility.IPlugin> ConfigureAndLoadPlugins(params string[] requestedPlugins) { }
    }
    public class Plugin : Orc.Extensibility.IPlugin
    {
        public Plugin(object instance, Orc.Extensibility.IPluginInfo info) { }
        public Orc.Extensibility.IPluginInfo Info { get; }
        public object Instance { get; }
        public override string ToString() { }
    }
    public class PluginCleanupService : Orc.Extensibility.IPluginCleanupService
    {
        public PluginCleanupService(Orc.FileSystem.IFileService fileService, Orc.FileSystem.IDirectoryService directoryService) { }
        public void Cleanup(string directory) { }
        public bool IsCleanupRequired(string directory) { }
    }
    public class PluginEventArgs : System.EventArgs
    {
        public PluginEventArgs(Orc.Extensibility.IPluginInfo pluginInfo, string messageTitle, string messageDetails) { }
        public PluginEventArgs(string pluginName, string messageTitle, string messageDetails) { }
        public string MessageDetails { get; set; }
        public string MessageTitle { get; }
        public Orc.Extensibility.IPluginInfo PluginInfo { get; }
        public string PluginName { get; }
    }
    public static class PluginExtensions
    {
        public static string GetAssemblyName(this Orc.Extensibility.IPluginInfo pluginInfo) { }
    }
    public class PluginFactory : Orc.Extensibility.IPluginFactory
    {
        public PluginFactory(Catel.IoC.ITypeFactory typeFactory) { }
        public virtual object CreatePlugin(Orc.Extensibility.IPluginInfo pluginInfo) { }
        protected virtual void PreloadAssembly(System.Reflection.Assembly assembly) { }
    }
    public abstract class PluginFinderBase : Orc.Extensibility.IPluginFinder
    {
        protected PluginFinderBase(Orc.Extensibility.IPluginLocationsProvider pluginLocationsProvider, Orc.Extensibility.IPluginInfoProvider pluginInfoProvider, Orc.Extensibility.IPluginCleanupService pluginCleanupService, Orc.FileSystem.IDirectoryService directoryService, Orc.FileSystem.IFileService fileService, Orc.Extensibility.IAssemblyReflectionService assemblyReflectionService) { }
        protected virtual bool CanInvestigateAssembly(Orc.Extensibility.PluginProbingContext context, System.Reflection.Assembly assembly) { }
        protected virtual bool CanInvestigateAssembly(Orc.Extensibility.PluginProbingContext context, string assemblyPath) { }
        public System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> FindPlugins() { }
        protected void FindPluginsInAssemblies(Orc.Extensibility.PluginProbingContext context, params string[] assemblyPaths) { }
        protected virtual void FindPluginsInAssembly(Orc.Extensibility.PluginProbingContext context, System.Reflection.Assembly assembly) { }
        protected virtual void FindPluginsInAssembly(Orc.Extensibility.PluginProbingContext context, string assemblyPath) { }
        protected virtual void FindPluginsInDirectory(Orc.Extensibility.PluginProbingContext context, string pluginDirectory) { }
        protected virtual void FindPluginsInLoadedAssemblies(Orc.Extensibility.PluginProbingContext context) { }
        protected virtual void FindPluginsInUnloadedAssemblies(Orc.Extensibility.PluginProbingContext context) { }
        protected virtual System.Collections.Generic.List<string> FindResolvableAssemblyPaths() { }
        protected virtual System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> GetOldestDuplicates(System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> duplicates) { }
        protected abstract bool IsPlugin(Orc.Extensibility.PluginProbingContext context, System.Type type);
        protected virtual void RemoveDuplicates(Orc.Extensibility.PluginProbingContext context) { }
        protected virtual bool ShouldIgnoreAssembly(string assemblyPath) { }
    }
    public class PluginInfo : Orc.Extensibility.IPluginInfo
    {
        public PluginInfo(string location, System.Type type) { }
        public System.Collections.Generic.List<string> Aliases { get; }
        public string AssemblyName { get; }
        public string Company { get; set; }
        public string Customer { get; set; }
        public string Description { get; set; }
        public string FullTypeName { get; }
        public string Location { get; }
        public string Name { get; set; }
        public string Version { get; set; }
        public override string ToString() { }
    }
    public class PluginInfoProvider : Orc.Extensibility.IPluginInfoProvider
    {
        public PluginInfoProvider() { }
        public virtual Orc.Extensibility.IPluginInfo GetPluginInfo(string location, System.Type type) { }
    }
    public class PluginLocationsProvider : Orc.Extensibility.IPluginLocationsProvider
    {
        public PluginLocationsProvider(Catel.Services.IAppDataService appDataService) { }
        public virtual System.Collections.Generic.IEnumerable<string> GetPluginDirectories() { }
        protected virtual bool ValidateDirectory(string directory) { }
    }
    public class PluginManager : Orc.Extensibility.IPluginManager
    {
        public PluginManager(Orc.Extensibility.IPluginFinder pluginFinder) { }
        public System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> GetPlugins(bool forceRefresh = false) { }
        public void Refresh() { }
    }
    public class PluginProbingContext
    {
        public PluginProbingContext() { }
        public System.Collections.Generic.HashSet<string> Locations { get; }
        public System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> Plugins { get; }
    }
    public static class ReflectionExtensions
    {
        public static bool ImplementsInterface<TInterface>(this System.Type type) { }
    }
    public class SinglePluginService : Orc.Extensibility.IPluginService, Orc.Extensibility.ISinglePluginService
    {
        public SinglePluginService(Orc.Extensibility.IPluginManager pluginManager, Orc.Extensibility.IPluginFactory pluginFactory, Orc.Extensibility.ILoadedPluginService loadedPluginService) { }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
        public Orc.Extensibility.IPlugin ConfigureAndLoadPlugin(string expectedPlugin, string defaultPlugin) { }
    }
}