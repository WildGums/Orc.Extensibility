namespace Orc.Extensibility
{
    public static class CosturaRuntimeAssemblyExtensions
    {
        public static void PreloadStream(this CosturaRuntimeAssembly runtimeAssembly)
        {
            using (var stream = runtimeAssembly.GetStream())
            {
                // Will be cached
            }
        }
    }
}
