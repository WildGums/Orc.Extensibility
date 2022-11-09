namespace Orc.Extensibility
{
    public static class CosturaRuntimeAssemblyExtensions
    {
        public static void PreloadStream(this ICosturaRuntimeAssembly runtimeAssembly)
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
