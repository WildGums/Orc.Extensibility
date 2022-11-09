namespace Orc.Extensibility
{
    using System;
    using System.Reflection;

    public class RuntimeLoadingAssemblyEventArgs : EventArgs
    {
        public RuntimeLoadingAssemblyEventArgs(AssemblyName requestedAssemblyName, 
            IRuntimeAssembly resolvedRuntimeAssembly)
        {
            RequestedAssemblyName = requestedAssemblyName;
            ResolvedRuntimeAssembly = resolvedRuntimeAssembly;
            Cancel = false;
        }

        public AssemblyName RequestedAssemblyName { get; }
        public IRuntimeAssembly ResolvedRuntimeAssembly { get; }

        public bool Cancel { get; set; }
    }
}
