namespace Orc.Extensibility
{
    using System;
    using System.Reflection;

    public class RuntimeLoadedAssemblyEventArgs : EventArgs
    {
        public RuntimeLoadedAssemblyEventArgs(AssemblyName requestedAssemblyName, RuntimeAssembly resolvedRuntimeAssembly, Assembly resolvedAssembly)
        {
            ArgumentNullException.ThrowIfNull(requestedAssemblyName);
            ArgumentNullException.ThrowIfNull(resolvedRuntimeAssembly);
            ArgumentNullException.ThrowIfNull(resolvedAssembly);

            RequestedAssemblyName = requestedAssemblyName;
            ResolvedRuntimeAssembly = resolvedRuntimeAssembly;
            ResolvedAssembly = resolvedAssembly;
        }

        public AssemblyName RequestedAssemblyName { get; }
        public RuntimeAssembly ResolvedRuntimeAssembly { get; }
        public Assembly ResolvedAssembly { get; }
    }
}
