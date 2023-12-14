namespace Orc.Extensibility;

using System;

public static class CosturaRuntimeAssemblyExtensions
{
    public static void PreloadStream(this ICosturaRuntimeAssembly runtimeAssembly)
    {
        ArgumentNullException.ThrowIfNull(runtimeAssembly);

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