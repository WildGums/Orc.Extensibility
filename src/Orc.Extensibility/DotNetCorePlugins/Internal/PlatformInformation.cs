// Note: comes from https://github.com/natemcmaster/DotNetCorePlugins/

namespace Orc.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Catel.Logging;

    internal static class PlatformInformation
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        public static readonly string[] RuntimeIdentifiers;
        public static readonly string[] NativeLibraryExtensions;
        public static readonly string[] NativeLibraryPrefixes;
        public static readonly string[] ManagedAssemblyExtensions = new[]
        {
                ".dll",
                ".ni.dll",
                ".exe",
                ".ni.exe"
        };

        static PlatformInformation()
        {
            var runtimeIdentifiers = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NativeLibraryPrefixes = new[] { "" };
                NativeLibraryExtensions = new[] { ".dll" };

                // For now we only care about windows
                runtimeIdentifiers.Add($"win-{RuntimeInformation.OSArchitecture.ToString().ToLower()}");
                runtimeIdentifiers.Add("win");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                NativeLibraryPrefixes = new[] { "", "lib", };
                NativeLibraryExtensions = new[] { ".dylib" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                NativeLibraryPrefixes = new[] { "", "lib" };
                NativeLibraryExtensions = new[] { ".so", ".so.1" };
            }
            else
            {
                Log.Error("Unknown or unsupported OS type");

                NativeLibraryPrefixes = Array.Empty<string>();
                NativeLibraryExtensions = Array.Empty<string>();
            }

            RuntimeIdentifiers = runtimeIdentifiers.ToArray();
        }
    }
}
