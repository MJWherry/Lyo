using System.Text;
using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;

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
            var probe = PathHelpers.Combine(fileSystem.PathStyle, path, $".rw-check-{Guid.NewGuid():N}.tmp");
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
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(data);
            using var stream = fileSystem.OpenCreate(path);
            stream.Write(data, 0, data.Length);
        }

        /// <summary>Writes text to <paramref name="path" /> with <paramref name="encoding" />.</summary>
        public void WriteAllText(string path, string text, Encoding encoding)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            ArgumentHelpers.ThrowIfNull(encoding);
            fileSystem.WriteAllBytes(path, encoding.GetBytes(text));
        }

        /// <summary>Appends text to <paramref name="path" /> with <paramref name="encoding" />.</summary>
        public void AppendAllText(string path, string text, Encoding encoding)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            ArgumentHelpers.ThrowIfNull(encoding);
            var bytes = encoding.GetBytes(text);
            using var stream = fileSystem.OpenAppend(path);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>Writes bytes to <paramref name="path" /> asynchronously.</summary>
        public async Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(data);
            using var stream = await fileSystem.OpenCreateAsync(path, ct).ConfigureAwait(false);
#if NETSTANDARD2_0
            await stream.WriteAsync(data, 0, data.Length, ct).ConfigureAwait(false);
#else
            await stream.WriteAsync(data.AsMemory(), ct).ConfigureAwait(false);
#endif
        }

        /// <summary>Writes text to <paramref name="path" /> asynchronously.</summary>
        public Task WriteAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            ArgumentHelpers.ThrowIfNull(encoding);
            return fileSystem.WriteAllBytesAsync(path, encoding.GetBytes(text), ct);
        }

        /// <summary>Appends text to <paramref name="path" /> asynchronously.</summary>
        public async Task AppendAllTextAsync(string path, string text, Encoding encoding, CancellationToken ct)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(text);
            ArgumentHelpers.ThrowIfNull(encoding);
            var bytes = encoding.GetBytes(text);
            using var stream = await fileSystem.OpenAppendAsync(path, ct).ConfigureAwait(false);
#if NETSTANDARD2_0
            await stream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
#else
            await stream.WriteAsync(bytes.AsMemory(), ct).ConfigureAwait(false);
#endif
        }

        /// <summary>Copies <paramref name="source" /> into <paramref name="destPath" />.</summary>
        public async Task CopyStreamToFileAsync(Stream source, string destPath, CancellationToken ct)
        {
            ArgumentHelpers.ThrowIfNull(fileSystem);
            ArgumentHelpers.ThrowIfNull(source);
            using var dest = await fileSystem.OpenCreateAsync(destPath, ct).ConfigureAwait(false);
#if NETSTANDARD2_0
            await source.CopyToAsync(dest, 81920, ct).ConfigureAwait(false);
#else
            await source.CopyToAsync(dest, ct).ConfigureAwait(false);
#endif
        }
    }
}
