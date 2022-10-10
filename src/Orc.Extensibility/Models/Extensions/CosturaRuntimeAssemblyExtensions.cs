namespace Orc.Extensibility
{
    using System;

    public static class CosturaRuntimeAssemblyExtensions
    {
        public static void PreloadStream(this CosturaRuntimeAssembly runtimeAssembly)
        {
            ArgumentNullException.ThrowIfNull(runtimeAssembly);

            using (var stream = runtimeAssembly.GetStream())
            {
                // Will be cached
            }
        }
    }
}
