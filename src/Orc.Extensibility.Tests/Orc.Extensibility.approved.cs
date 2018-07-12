[assembly: System.Resources.NeutralResourcesLanguageAttribute("en-US")]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.6", FrameworkDisplayName=".NET Framework 4.6")]
public class static ModuleInitializer
{
    public static void Initialize() { }
}
namespace Orc.Extensibility
{
    public class static CustomAttributeDataExtensions
    {
        public static object GetReflectionOnlyAttributeValue<TAttribute>(this System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> customAttributes)
            where TAttribute : System.Attribute { }
    }
    public interface ILoadedPluginService
    {
        [System.ObsoleteAttribute("Use `GetLoadedPlugins` instead. Will be removed in version 4.0.0.", true)]
        System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> LoadedPlugins { get; }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
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
    public class static IPluginFinderExtensions { }
    public interface IPluginInfo
    {
        string Company { get; set; }
        string Customer { get; set; }
        string Description { get; set; }
        string FullTypeName { get; }
        string Location { get; }
        string Name { get; set; }
        System.Type ReflectionOnlyType { get; }
        string Version { get; set; }
    }
    public interface IPluginInfoProvider
    {
        Orc.Extensibility.IPluginInfo GetPluginInfo(System.Type pluginType);
    }
    public interface IPluginLocationsProvider
    {
        System.Collections.Generic.IEnumerable<string> GetPluginDirectories();
    }
    public interface IPluginManager
    {
        System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> GetPlugins(bool forceRefresh = False);
    }
    public class static IPluginManagerExtensions
    {
        public static System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> FindPluginImplementations<TPlugin>(this Orc.Extensibility.IPluginManager pluginManager) { }
        public static System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> FindPluginImplementations(this Orc.Extensibility.IPluginManager pluginManager, System.Type interfaceType) { }
    }
    public interface IPluginService
    {
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
    }
    public interface ISinglePluginService : Orc.Extensibility.IPluginService
    {
        Orc.Extensibility.IPlugin ConfigureAndLoadPlugin(string expectedPlugin, string defaultPlugin);
    }
    public class LoadedPluginService : Orc.Extensibility.ILoadedPluginService
    {
        public LoadedPluginService() { }
        [System.ObsoleteAttribute("Use `GetLoadedPlugins` instead. Will be removed in version 4.0.0.", true)]
        public System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> LoadedPlugins { get; }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public void AddPlugin(Orc.Extensibility.IPluginInfo pluginInfo) { }
        public System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> GetLoadedPlugins() { }
    }
    public class MultiplePluginsService : Orc.Extensibility.IMultiplePluginsService, Orc.Extensibility.IPluginService
    {
        public MultiplePluginsService(Orc.Extensibility.IPluginManager pluginManager, Orc.Extensibility.IPluginFactory pluginFactory, Orc.Extensibility.ILoadedPluginService loadedPluginService) { }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
        public System.Collections.Generic.IEnumerable<Orc.Extensibility.IPlugin> ConfigureAndLoadPlugins(params string[] requestedPlugins) { }
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
    public class static PluginExtensions
    {
        public static string GetAssemblyName(this Orc.Extensibility.IPluginInfo pluginInfo) { }
    }
    public class PluginFactory : Orc.Extensibility.IPluginFactory
    {
        public PluginFactory(Catel.IoC.ITypeFactory typeFactory) { }
        public object CreatePlugin(Orc.Extensibility.IPluginInfo pluginInfo) { }
    }
    public abstract class PluginFinderBase : Orc.Extensibility.IPluginFinder
    {
        protected PluginFinderBase(Orc.Extensibility.IPluginLocationsProvider pluginLocationsProvider, Orc.Extensibility.IPluginInfoProvider pluginInfoProvider, Orc.Extensibility.IPluginCleanupService pluginCleanupService, Orc.FileSystem.IDirectoryService directoryService, Orc.FileSystem.IFileService fileService) { }
        public System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> FindPlugins() { }
        protected System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> FindPluginsInAssemblies(params string[] assemblyPaths) { }
        protected System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> FindPluginsInAssembly(System.Reflection.Assembly assembly) { }
        protected System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> FindPluginsInDirectory(string pluginDirectory) { }
        protected abstract bool IsPlugin(System.Type type);
        protected virtual bool ShouldIgnoreAssembly(string assemblyPath) { }
    }
    public class PluginInfo : Orc.Extensibility.IPluginInfo
    {
        public PluginInfo(System.Type type) { }
        public string Company { get; set; }
        public string Customer { get; set; }
        public string Description { get; set; }
        public string FullTypeName { get; }
        public string Location { get; }
        public string Name { get; set; }
        public System.Type ReflectionOnlyType { get; }
        public string Version { get; set; }
        public override string ToString() { }
    }
    public class PluginInfoProvider : Orc.Extensibility.IPluginInfoProvider
    {
        public PluginInfoProvider() { }
        public virtual Orc.Extensibility.IPluginInfo GetPluginInfo(System.Type pluginType) { }
    }
    public class PluginLocationsProvider : Orc.Extensibility.IPluginLocationsProvider
    {
        public PluginLocationsProvider() { }
        public virtual System.Collections.Generic.IEnumerable<string> GetPluginDirectories() { }
        protected virtual bool ValidateDirectory(string directory) { }
    }
    public class PluginManager : Orc.Extensibility.IPluginManager
    {
        public PluginManager(Orc.Extensibility.IPluginFinder pluginFinder) { }
        public System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> GetPlugins(bool forceRefresh = False) { }
    }
    public class SinglePluginService : Orc.Extensibility.IPluginService, Orc.Extensibility.ISinglePluginService
    {
        public SinglePluginService(Orc.Extensibility.IPluginManager pluginManager, Orc.Extensibility.IPluginFactory pluginFactory, Orc.Extensibility.ILoadedPluginService loadedPluginService) { }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
        public Orc.Extensibility.IPlugin ConfigureAndLoadPlugin(string expectedPlugin, string defaultPlugin) { }
    }
}