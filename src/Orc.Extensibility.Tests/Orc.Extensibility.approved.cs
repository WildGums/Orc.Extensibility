[assembly: System.Resources.NeutralResourcesLanguage("en-US")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Orc.Extensibility.Tests")]
[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName="")]
public static class ModuleInitializer
{
    public static void Initialize() { }
}
namespace Orc.Extensibility
{
    public class AppDomainRuntimeAssemblyWatcher
    {
        public AppDomainRuntimeAssemblyWatcher(Orc.Extensibility.IRuntimeAssemblyResolverService runtimeAssemblyResolverService, Catel.Services.IAppDataService appDataService, Orc.FileSystem.IDirectoryService directoryService, Orc.FileSystem.IFileService fileService) { }
        public bool AllowAssemblyResolvingFromOtherLoadContexts { get; set; }
        public System.Collections.Generic.List<Orc.Extensibility.RuntimeAssembly> LoadedAssemblies { get; }
        public event System.EventHandler<Orc.Extensibility.RuntimeLoadedAssemblyEventArgs> AssemblyLoaded;
        public void Attach() { }
        public void Attach(System.Runtime.Loader.AssemblyLoadContext assemblyLoadContext) { }
    }
    public class AssemblyReflectionService : Orc.Extensibility.IAssemblyReflectionService
    {
        public AssemblyReflectionService(Orc.FileSystem.IFileService fileService) { }
        public virtual bool IsPeAssembly(string assemblyPath) { }
    }
    public class CosturaRuntimeAssembly : Orc.Extensibility.RuntimeAssembly
    {
        public CosturaRuntimeAssembly(Orc.Extensibility.EmbeddedResource embeddedResource) { }
        public CosturaRuntimeAssembly(string content) { }
        public string AssemblyName { get; set; }
        public Orc.Extensibility.EmbeddedResource EmbeddedResource { get; set; }
        public override bool IsRuntime { get; }
        public string RelativeFileName { get; set; }
        public string ResourceName { get; set; }
        public long? Size { get; set; }
        public string Version { get; set; }
        public override System.IO.Stream GetStream() { }
        protected override void OnMarkLoaded() { }
        public override string ToString() { }
    }
    public static class CosturaRuntimeAssemblyExtensions
    {
        public static void PreloadStream(this Orc.Extensibility.CosturaRuntimeAssembly runtimeAssembly) { }
    }
    public static class CustomAttributeDataExtensions
    {
        public static object GetAttributeValue<TAttribute>(this System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> customAttributes)
            where TAttribute : System.Attribute { }
        public static System.Collections.Generic.List<object> GetAttributeValues<TAttribute>(this System.Collections.Generic.IEnumerable<System.Reflection.CustomAttributeData> customAttributes)
            where TAttribute : System.Attribute { }
    }
    public class EmbeddedResource
    {
        public EmbeddedResource() { }
        public Orc.Extensibility.RuntimeAssembly ContainerAssembly { get; set; }
        public string Name { get; set; }
        public int Size { get; set; }
        public unsafe byte* Start { get; set; }
        public override string ToString() { }
    }
    public class FileRuntimeAssembly : Orc.Extensibility.RuntimeAssembly
    {
        public FileRuntimeAssembly(string location) { }
        public FileRuntimeAssembly(string name, string source, string checksum, string location) { }
        public string Location { get; }
        public override System.IO.Stream GetStream() { }
        protected override void OnMarkLoaded() { }
        public override string ToString() { }
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
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Orc.Extensibility.IPlugin>> ConfigureAndLoadPluginsAsync(params string[] requestedPlugins);
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
        System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo>> FindPluginsAsync();
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
        System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> GetPlugins();
        System.Threading.Tasks.Task RefreshAsync();
    }
    public static class IPluginManagerExtensions
    {
        public static System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo>> RefreshAndGetPluginsAsync(this Orc.Extensibility.IPluginManager pluginManager) { }
    }
    public interface IPluginService
    {
        event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
    }
    public interface IRuntimeAssemblyResolverService
    {
        Orc.Extensibility.PluginLoadContext[] GetPluginLoadContexts();
        System.Threading.Tasks.Task RegisterAssemblyAsync(Orc.Extensibility.RuntimeAssembly runtimeAssembly);
        System.Threading.Tasks.Task UnregisterAssemblyAsync(Orc.Extensibility.RuntimeAssembly runtimeAssembly);
    }
    public interface ISinglePluginService : Orc.Extensibility.IPluginService
    {
        System.Threading.Tasks.Task<Orc.Extensibility.IPlugin> ConfigureAndLoadPluginAsync(string expectedPlugin, string defaultPlugin);
        System.Threading.Tasks.Task SetFallbackPluginAsync(Orc.Extensibility.IPluginInfo fallbackPlugin);
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
        protected virtual System.Threading.Tasks.Task<Orc.Extensibility.Plugin> ConfigureAndLoadPluginAsync(Orc.Extensibility.IPluginInfo pluginToLoad, bool isLastTry) { }
        public virtual System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Orc.Extensibility.IPlugin>> ConfigureAndLoadPluginsAsync(params string[] requestedPlugins) { }
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
        public PluginFactory(Catel.IoC.ITypeFactory typeFactory, Orc.Extensibility.IRuntimeAssemblyResolverService runtimeAssemblyResolverService) { }
        public virtual object CreatePlugin(Orc.Extensibility.IPluginInfo pluginInfo) { }
        protected virtual void PreloadAssembly(System.Reflection.Assembly assembly) { }
    }
    public abstract class PluginFinderBase : Orc.Extensibility.IPluginFinder
    {
        protected PluginFinderBase(Orc.Extensibility.IPluginLocationsProvider pluginLocationsProvider, Orc.Extensibility.IPluginInfoProvider pluginInfoProvider, Orc.Extensibility.IPluginCleanupService pluginCleanupService, Orc.FileSystem.IDirectoryService directoryService, Orc.FileSystem.IFileService fileService, Orc.Extensibility.IAssemblyReflectionService assemblyReflectionService, Orc.Extensibility.IRuntimeAssemblyResolverService runtimeAssemblyResolverService) { }
        protected virtual bool CanInvestigateAssembly(Orc.Extensibility.PluginProbingContext context, System.Reflection.Assembly assembly) { }
        protected virtual bool CanInvestigateAssembly(Orc.Extensibility.PluginProbingContext context, string assemblyPath) { }
        public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo>> FindPluginsAsync() { }
        protected System.Threading.Tasks.Task FindPluginsInAssembliesAsync(Orc.Extensibility.PluginProbingContext context, params string[] assemblyPaths) { }
        protected virtual System.Threading.Tasks.Task FindPluginsInAssemblyAsync(Orc.Extensibility.PluginProbingContext context, System.Reflection.Assembly assembly) { }
        protected virtual System.Threading.Tasks.Task FindPluginsInAssemblyAsync(Orc.Extensibility.PluginProbingContext context, string assemblyPath) { }
        protected virtual System.Threading.Tasks.Task FindPluginsInDirectoryAsync(Orc.Extensibility.PluginProbingContext context, string pluginDirectory) { }
        protected virtual System.Threading.Tasks.Task FindPluginsInLoadedAssembliesAsync(Orc.Extensibility.PluginProbingContext context) { }
        protected virtual System.Threading.Tasks.Task FindPluginsInUnloadedAssembliesAsync(Orc.Extensibility.PluginProbingContext context) { }
        protected virtual System.Collections.Generic.List<string> FindResolvableAssemblyPaths(string assemblyPath) { }
        protected virtual System.Version GetFileVersion(string fileName) { }
        protected virtual System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> GetOldestDuplicates(System.Collections.Generic.List<Orc.Extensibility.IPluginInfo> duplicates) { }
        protected abstract bool IsPlugin(Orc.Extensibility.PluginProbingContext context, System.Type type);
        protected virtual bool IsPluginFastPreCheck(Orc.Extensibility.PluginProbingContext context, System.Type type) { }
        protected virtual bool IsSigned(string fileName, string subjectName = null) { }
        protected virtual System.Threading.Tasks.Task RemoveDuplicatesAsync(Orc.Extensibility.PluginProbingContext context) { }
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
    public class PluginLoadContext
    {
        public PluginLoadContext(Orc.Extensibility.RuntimeAssembly pluginRuntimeAssembly) { }
        public Orc.Extensibility.RuntimeAssembly PluginRuntimeAssembly { get; }
        public System.Collections.Generic.List<Orc.Extensibility.RuntimeAssembly> RuntimeAssemblies { get; }
        public override string ToString() { }
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
        public System.Collections.Generic.IEnumerable<Orc.Extensibility.IPluginInfo> GetPlugins() { }
        public System.Threading.Tasks.Task RefreshAsync() { }
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
    public abstract class RuntimeAssembly
    {
        protected RuntimeAssembly() { }
        protected RuntimeAssembly(string name, string source, string checksum) { }
        public string Checksum { get; set; }
        public System.Collections.Generic.List<Orc.Extensibility.RuntimeAssembly> Dependencies { get; }
        public bool IsLoaded { get; set; }
        public virtual bool IsRuntime { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public abstract System.IO.Stream GetStream();
        public void MarkLoaded() { }
        protected abstract void OnMarkLoaded();
        public override string ToString() { }
    }
    public class RuntimeAssemblyResolverService : Orc.Extensibility.IRuntimeAssemblyResolverService
    {
        public RuntimeAssemblyResolverService(Orc.FileSystem.IFileService fileService, Orc.FileSystem.IDirectoryService directoryService, Orc.Extensibility.IAssemblyReflectionService assemblyReflectionService, Catel.Services.IAppDataService appDataService) { }
        protected virtual System.Threading.Tasks.Task<System.Collections.Generic.List<Orc.Extensibility.CosturaRuntimeAssembly>> FindEmbeddedAssembliesViaMetadataAsync(System.Collections.Generic.IEnumerable<Orc.Extensibility.EmbeddedResource> resources) { }
        protected System.Threading.Tasks.Task<System.Collections.Generic.List<Orc.Extensibility.EmbeddedResource>> FindEmbeddedResourcesAsync(System.Reflection.PortableExecutable.PEReader peReader, Orc.Extensibility.RuntimeAssembly containerAssembly) { }
        public Orc.Extensibility.PluginLoadContext[] GetPluginLoadContexts() { }
        protected virtual System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<Orc.Extensibility.RuntimeAssembly>> IndexCosturaEmbeddedAssembliesAsync(Orc.Extensibility.PluginLoadContext pluginLoadContext, Orc.Extensibility.RuntimeAssembly originatingAssembly, Orc.Extensibility.RuntimeAssembly runtimeAssembly) { }
        public System.Threading.Tasks.Task RegisterAssemblyAsync(Orc.Extensibility.RuntimeAssembly runtimeAssembly) { }
        protected System.Threading.Tasks.Task RegisterAssemblyAsync(Orc.Extensibility.PluginLoadContext pluginLoadContext, Orc.Extensibility.RuntimeAssembly originatingAssembly, Orc.Extensibility.RuntimeAssembly runtimeAssembly) { }
        protected virtual bool ShouldIgnoreAssemblyForCosturaExtracting(Orc.Extensibility.PluginLoadContext pluginLoadContext, Orc.Extensibility.RuntimeAssembly originatingAssembly, Orc.Extensibility.RuntimeAssembly runtimeAssembly) { }
        public System.Threading.Tasks.Task UnregisterAssemblyAsync(Orc.Extensibility.RuntimeAssembly runtimeAssembly) { }
    }
    public class RuntimeLoadedAssemblyEventArgs : System.EventArgs
    {
        public RuntimeLoadedAssemblyEventArgs(System.Reflection.AssemblyName requestedAssemblyName, Orc.Extensibility.RuntimeAssembly resolvedRuntimeAssembly, System.Reflection.Assembly resolvedAssembly) { }
        public System.Reflection.AssemblyName RequestedAssemblyName { get; }
        public System.Reflection.Assembly ResolvedAssembly { get; }
        public Orc.Extensibility.RuntimeAssembly ResolvedRuntimeAssembly { get; }
    }
    public class SinglePluginService : Orc.Extensibility.IPluginService, Orc.Extensibility.ISinglePluginService
    {
        public SinglePluginService(Orc.Extensibility.IPluginManager pluginManager, Orc.Extensibility.IPluginFactory pluginFactory, Orc.Extensibility.ILoadedPluginService loadedPluginService) { }
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoaded;
        public event System.EventHandler<Orc.Extensibility.PluginEventArgs> PluginLoadingFailed;
        public System.Threading.Tasks.Task<Orc.Extensibility.IPlugin> ConfigureAndLoadPluginAsync(string expectedPlugin, string defaultPlugin) { }
        public System.Threading.Tasks.Task SetFallbackPluginAsync(Orc.Extensibility.IPluginInfo fallbackPlugin) { }
    }
}