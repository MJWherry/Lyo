using System.Text;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.Common.Core.Identifiers;

namespace Lyo.IO.FileSystem;

/// <summary>Convenience helpers that IOTemp and similar callers used on the old storage-provider surface.</summary>
public static class FileSystemExtensions
{
    extension(IFileSystem fileSystem)
    {
        /// <summary>Creates an empty file at <paramref name="path" />, replacing any file already there.</summary>
        public void TouchFile(string path)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            using var _ = fileSystem.OpenCreate(path);
        }

        /// <summary>
        /// Checks that <paramref name="path" /> can be read and written. Throw on access denied. In-memory backends may no-op when they cannot probe.
        /// </summary>
        public void EnsureDirectoryAccessible(string path)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            if ((fileSystem.Capabilities & FileSystemCapabilities.Write) == 0)
                return;

            fileSystem.CreateDirectory(path);
            var probe = PathHelpers.Combine(fileSystem.PathStyle, path, $".rw-check-{LyoGuid.CreateV7():N}.tmp");
            try {
                fileSystem.WriteAllBytes(probe, [.. "rw"u8]);
            }
            finally {
                if (fileSystem.FileExists(probe))
                    fileSystem.DeleteFile(probe);
            }
        }

        /// <summary>Writes bytes to <paramref name="path" />, replacing any existing file.</summary>
        public void WriteAllBytes(string path, byte[] data)
        {
            ArgumentHelpers.ThrowIfNull(data);
            fileSystem.WriteAllBytes(path, data.AsMemory());
        }

        /// <summary>Writes bytes to <paramref name="path" />, replacing any existing file. Takes a slice without copying it first.</summary>
        public void WriteAllBytes(string path, ReadOnlyMemory<byte> data)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            using var stream = fileSystem.OpenCreate(path);
#if NETSTANDARD2_0
            var segment = data.ToArray();
            stream.Write(segment, 0, segment.Length);
#else
            stream.Write(data.Span);
#endif
        }

        /// <summary>Reads the whole file at <paramref name="path" />. Throws <see cref="FileNotFoundException" /> when the file is missing.</summary>
        public byte[] ReadAllBytes(string path)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            using var stream = fileSystem.OpenRead(path);
            return DrainToArray(stream);
        }

        /// <summary>Reads the whole file at <paramref name="path" /> and decodes it with <paramref name="encoding" />.</summary>
        public string ReadAllText(string path, Encoding? encoding = null)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            return (encoding ?? Encoding.UTF8).GetString(fileSystem.ReadAllBytes(path));
        }

        /// <summary>Reads the whole file at <paramref name="path" />. Throws <see cref="FileNotFoundException" /> when the file is missing.</summary>
        public async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
#if NETSTANDARD2_0
            using var stream = await fileSystem.OpenReadAsync(path, ct).ConfigureAwait(false);
#else
            await using var stream = await fileSystem.OpenReadAsync(path, ct).ConfigureAwait(false);
#endif
            return await DrainToArrayAsync(stream, ct).ConfigureAwait(false);
        }

        /// <summary>Reads the whole file at <paramref name="path" /> and decodes it with <paramref name="encoding" />.</summary>
        public async Task<string> ReadAllTextAsync(string path, Encoding? encoding = null, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            return (encoding ?? Encoding.UTF8).GetString(await fileSystem.ReadAllBytesAsync(path, ct).ConfigureAwait(false));
        }

        /// <summary>Writes text to <paramref name="path" />, defaulting to UTF-8.</summary>
        public void WriteAllText(string path, string text, Encoding? encoding = null)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            fileSystem.WriteAllBytes(path, (encoding ?? Encoding.UTF8).GetBytes(text));
        }

        /// <summary>Appends text to <paramref name="path" />, defaulting to UTF-8.</summary>
        public void AppendAllText(string path, string text, Encoding? encoding = null)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(text);
            using var stream = fileSystem.OpenAppend(path);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>Writes bytes to <paramref name="path" /> asynchronously, replacing any existing file.</summary>
        public Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(data);
            return fileSystem.WriteAllBytesAsync(path, data.AsMemory(), ct);
        }

        /// <summary>Writes bytes to <paramref name="path" /> asynchronously, replacing any existing file. Takes a slice without copying it first.</summary>
        public async Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> data, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
#if NETSTANDARD2_0
            using var stream = await fileSystem.OpenCreateAsync(path, ct).ConfigureAwait(false);
            var segment = data.ToArray();
            await stream.WriteAsync(segment, 0, segment.Length, ct).ConfigureAwait(false);
#else
            await using var stream = await fileSystem.OpenCreateAsync(path, ct).ConfigureAwait(false);
            await stream.WriteAsync(data, ct).ConfigureAwait(false);
#endif
        }

        /// <summary>Writes text to <paramref name="path" /> asynchronously, defaulting to UTF-8.</summary>
        public Task WriteAllTextAsync(string path, string text, Encoding? encoding = null, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            return fileSystem.WriteAllBytesAsync(path, (encoding ?? Encoding.UTF8).GetBytes(text).AsMemory(), ct);
        }

        /// <summary>Appends text to <paramref name="path" /> asynchronously, defaulting to UTF-8.</summary>
        public async Task AppendAllTextAsync(string path, string text, Encoding? encoding = null, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            var bytes = (encoding ?? Encoding.UTF8).GetBytes(text);
#if NETSTANDARD2_0
            using var stream = await fileSystem.OpenAppendAsync(path, ct).ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
#else
            await using var stream = await fileSystem.OpenAppendAsync(path, ct).ConfigureAwait(false);
            await stream.WriteAsync(bytes.AsMemory(), ct).ConfigureAwait(false);
#endif
        }

        /// <summary>Copies <paramref name="source" /> into <paramref name="destPath" />.</summary>
        public async Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct = default)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(source);
#if NETSTANDARD2_0
            using var dest = await fileSystem.OpenCreateAsync(destPath, ct).ConfigureAwait(false);
            await source.CopyToAsync(dest, 81920, ct).ConfigureAwait(false);
#else
            await using var dest = await fileSystem.OpenCreateAsync(destPath, ct).ConfigureAwait(false);
            await source.CopyToAsync(dest, ct).ConfigureAwait(false);
#endif
        }
    }

    private const int CopyBufferSize = 81920;

    private static byte[] DrainToArray(Stream stream)
    {
        if (stream is MemoryStream memory)
            return memory.ToArray();

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer, CopyBufferSize);
        return buffer.ToArray();
    }

    private static async Task<byte[]> DrainToArrayAsync(Stream stream, CancellationToken ct)
    {
        if (stream is MemoryStream memory)
            return memory.ToArray();

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, CopyBufferSize, ct).ConfigureAwait(false);
        return buffer.ToArray();
    }
}
