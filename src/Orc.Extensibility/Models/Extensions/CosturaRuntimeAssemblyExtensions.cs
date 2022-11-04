namespace Orc.Extensibility
{
    public static class CosturaRuntimeAssemblyExtensions
    {
        public static void PreloadStream(this CosturaRuntimeAssembly runtimeAssembly)
        {
            if (runtimeAssembly.IsLoaded)
            {
                return;
            }

            using (var stream = runtimeAssembly.GetStream())
            {
                // Will be cached
            }
        }
    }
}
