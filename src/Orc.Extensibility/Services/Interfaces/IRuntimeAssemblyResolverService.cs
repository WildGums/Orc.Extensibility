namespace Orc.Extensibility
{
    public interface IRuntimeAssemblyResolverService
    {
        string TargetDirectory { get; }
        RuntimeAssembly[] GetRuntimeAssemblies();
        void RegisterAssembly(string assemblyLocation);
    }
}
