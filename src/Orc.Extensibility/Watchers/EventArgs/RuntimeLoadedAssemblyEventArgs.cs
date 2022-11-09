namespace Orc.Extensibility
{
    using System;
    using System.Reflection;

    public class RuntimeLoadedAssemblyEventArgs : EventArgs
    {
        public RuntimeLoadedAssemblyEventArgs(AssemblyName requestedAssemblyName, IRuntimeAssembly resolvedRuntimeAssembly, Assembly resolvedAssembly)
        {
            RequestedAssemblyName = requestedAssemblyName;
            ResolvedRuntimeAssembly = resolvedRuntimeAssembly;
            ResolvedAssembly = resolvedAssembly;
        }

        public AssemblyName RequestedAssemblyName { get; }
        public IRuntimeAssembly ResolvedRuntimeAssembly { get; }
        public Assembly ResolvedAssembly { get; }
    }
}
