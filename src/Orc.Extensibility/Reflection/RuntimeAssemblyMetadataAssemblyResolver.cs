namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// This class is written in preparation in case we need to lazy-load dynamic assemblies for assembly resolving. For
    /// now it provides the same behavior as the <see cref="PathAssemblyResolver"/>, but can be improved in the future.
    /// </summary>
    internal class RuntimeAssemblyMetadataAssemblyResolver : PathAssemblyResolver
    {
        private readonly List<RuntimeAssembly> _runtimeAssemblies = new List<RuntimeAssembly>();

        public RuntimeAssemblyMetadataAssemblyResolver(IEnumerable<string> assemblyPaths)
            : base(assemblyPaths)
        {
        }

        public void RegisterAssembly(RuntimeAssembly runtimeAssembly)
        {
            _runtimeAssemblies.Add(runtimeAssembly);
        }

        public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            // Note: 

            //var runtimeAssembly = (from x in _runtimeAssemblies
            //                      where x.Name == assemblyName.Name
            //                      select x).FirstOrDefault();
            //if (runtimeAssembly is not null)
            //{

            //}

            // fallback to base
            return base.Resolve(context, assemblyName);
        }

        private Assembly LoadRuntimeAssembly(MetadataLoadContext context, AssemblyName assemblyName, RuntimeAssembly runtimeAssembly)
        {
            // TODO: Check container assembly, load that if needed, etc

            using (var stream = runtimeAssembly.GetStream())
            {
                var assembly = context.LoadFromStream(stream);
                return assembly;
            }
        }
    }
}
