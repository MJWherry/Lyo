using Lyo.Common.Core.Pathing;
using Lyo.Exceptions;
using Lyo.FileStorage.Abstractions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage;

/// <summary>Opens a raw physical object by key. Hosts that must not reference <see cref="IFileSystem" /> should call this instead of <see cref="IFileStoragePhysical.Physical" />.</summary>
public static class FileStoragePhysicalRead
{
    /// <summary>
    /// Opens the object at <paramref name="physicalKey" /> (relative to the backend jail). Does not decrypt or decompress. Throws when the backend has no physical
    /// listing, the key is unsafe, or the file is missing.
    /// </summary>
    public static async Task<(Stream Stream, string FileName)> OpenAsync(
        IFileStorageService fileStorage,
        string physicalKey,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fileStorage);
        var key = FileHelpers.NormalizeAndValidatePathPrefix(physicalKey);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(key);
        if (fileStorage is not IFileStoragePhysical listing)
            throw new InvalidOperationException("This storage backend cannot read physical keys.");

        var fs = listing.Physical;
        var parts = key.Replace('\\', '/').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        var segments = new string[parts.Length + 1];
        segments[0] = fs.RootPath;
        Array.Copy(parts, 0, segments, 1, parts.Length);
        var path = PathHelpers.Combine(fs.PathStyle, segments);
        if (!await fs.FileExistsAsync(path, ct).ConfigureAwait(false))
            throw new FileNotFoundException($"Physical object not found: {key}", key);

        var stream = await fs.OpenReadAsync(path, ct).ConfigureAwait(false);
        return (stream, PathHelpers.GetFileName(fs.PathStyle, path));
    }
}
