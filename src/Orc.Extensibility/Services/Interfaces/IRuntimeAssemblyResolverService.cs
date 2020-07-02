namespace Orc.Extensibility
{
    public interface IRuntimeAssemblyResolverService
    {
        RuntimeAssembly[] GetRuntimeAssemblies();
        void RegisterAssembly(string assemblyLocation);
    }
}