using Lyo.Common.Core.Pathing;
using Lyo.FileStorage.Abstractions;

namespace Lyo.FileStorage;

/// <summary>Opens a raw physical object by key. Hosts that must not reference <see cref="IFileSystem" /> should call this instead of <see cref="IFileStoragePhysical.Physical" />.</summary>
public static class FileStoragePhysicalRead
{
    /// <summary>
    /// Opens the object at <paramref name="physicalKey" /> (relative to the backend jail). Does not decrypt or decompress. Throws when the backend has no physical listing, the
    /// key is unsafe, or the file is missing.
    /// </summary>
    public static async Task<(Stream Stream, string FileName)> OpenAsync(IFileStorageService fileStorage, string physicalKey, CancellationToken ct = default)
    {
        var (fs, path, _) = await FileStoragePhysicalPath.ResolveExistingAsync(fileStorage, physicalKey, ct).ConfigureAwait(false);
        var stream = await fs.OpenReadAsync(path, ct).ConfigureAwait(false);
        return (stream, PathHelpers.GetFileName(fs.PathStyle, path));
    }
}