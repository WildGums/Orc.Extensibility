namespace Orc.Extensibility;

using System;
using System.IO;
using System.IO.Compression;
using Catel;
using Catel.Logging;
using Catel.Reflection;

public class CosturaRuntimeAssembly : RuntimeAssembly, ICosturaRuntimeAssembly
{
    private static readonly ILog Log = LogManager.GetCurrentClassLogger();

    private byte[]? _cachedData;

    public CosturaRuntimeAssembly(string content)
    {
        var splitted = content.Split('|');

        ResourceName = splitted[0];
        Version = splitted[1];
        AssemblyName = splitted[2];
        RelativeFileName = splitted[3];
        Checksum = splitted[4];

        // Requires newer version of Costura
        if (splitted.Length > 5 &&
            long.TryParse(splitted[5], out var size))
        {
            Size = size;
        }

        if (!string.IsNullOrWhiteSpace(AssemblyName))
        {
            Name = TypeHelper.GetAssemblyNameWithoutOverhead(AssemblyName);
        }
        else
        {
            Name = Path.GetFileName(RelativeFileName);
        }

        Source = AssemblyName;
    }

    public string ResourceName { get; set; }

    public string Version { get; set; }

    public string AssemblyName { get; set; }

    public string RelativeFileName { get; set; }

    public long? Size { get; set; }

    public EmbeddedResource? EmbeddedResource { get; set; }

    public override bool IsRuntime
    {
        get
        {
            var resourceName = ResourceName;
            if (!string.IsNullOrWhiteSpace(resourceName))
            {
                return resourceName.ContainsIgnoreCase(".runtimes.");
            }

            return EmbeddedResource?.Name.ContainsIgnoreCase(".runtimes.") ?? false;
        }
    }

    public override Stream GetStream()
    {
        if (IsLoaded)
        {
            throw Log.ErrorAndCreateException<NotSupportedException>($"{this} is marked as loaded, stream is no longer available");
        }

        if (_cachedData is null)
        {
            // Note: we preferred not to cache, but we can't read the same stream twice it seems
            var embeddedResource = EmbeddedResource;
            if (embeddedResource is null)
            {
                throw Log.ErrorAndCreateException<NotSupportedException>("Cannot get stream when the EmbeddedResource property is not set");
            }

            unsafe
            {
                using (var resourceStream = new UnmanagedMemoryStream(embeddedResource.Start, embeddedResource.Size))
                {
                    using (var stream = LoadStream(resourceStream, embeddedResource.Name))
                    {
                        _cachedData = ReadStream(stream);
                    }
                }
            }
        }

        return new MemoryStream(_cachedData);
    }

    protected override void OnMarkLoaded()
    {
        if (_cachedData is not null)
        {
            Log.Debug($"Releasing '{_cachedData.Length}' bytes of cached memory for {this}");

            _cachedData = null;
        }
    }

    public override string ToString()
    {
        return $"{ResourceName}|{Version}|{AssemblyName}|{RelativeFileName}|{Checksum}|{Size}";
    }

#pragma warning disable IDISP015 // Member should not return created and cached instance
    private Stream LoadStream(Stream existingStream, string resourceName)
#pragma warning restore IDISP015 // Member should not return created and cached instance
    {
        if (resourceName.EndsWith(".compressed"))
        {
            var originalPosition = existingStream.Position;

            using (var source = new DeflateStream(existingStream, CompressionMode.Decompress))
            {
                var memoryStream = new MemoryStream();

                CopyTo(source, memoryStream);

                memoryStream.Position = 0L;
                existingStream.Position = originalPosition;

                return memoryStream;
            }

        }

        return existingStream;
    }

    private void CopyTo(Stream source, Stream destination)
    {
        var array = new byte[81920];
        int count;

        while ((count = source.Read(array, 0, array.Length)) != 0)
        {
            destination.Write(array, 0, count);
        }

        destination.Flush();
    }

    private byte[] ReadStream(Stream stream)
    {
        var array = new byte[stream.Length];

#if NET8_0_OR_GREATER
        stream.ReadExactly(array);
#else
#pragma warning disable CA2022 // Avoid inexact read with 'Stream.Read'
        stream.Read(array, 0, array.Length);
#pragma warning restore CA2022 // Avoid inexact read with 'Stream.Read'
#endif
        return array;
    }
}
