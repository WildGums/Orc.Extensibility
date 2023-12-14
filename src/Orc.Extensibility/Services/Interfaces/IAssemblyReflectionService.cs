namespace Orc.Extensibility;

public interface IAssemblyReflectionService
{
    bool IsPeAssembly(string assemblyPath);
    void RegisterAssembly(string assemblyPath, bool isPeAssembly);
}