using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileMetadataStore.Models;
using Lyo.FileStorage.Abstractions;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage;

/// <summary>Zips every non-deleted file under a path prefix by listing them, then calling the archive-by-id API.</summary>
public sealed class FileStorageFolderArchive
{
    private readonly IFileMetadataStore _metadata;
    private readonly IFileStorageArchiveService _archives;

    /// <summary>Uses <paramref name="metadata" /> to list the folder and <paramref name="archives" /> to zip those ids.</summary>
    public FileStorageFolderArchive(IFileMetadataStore metadata, IFileStorageArchiveService archives)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        ArgumentHelpers.ThrowIfNull(archives);
        _metadata = metadata;
        _archives = archives;
    }

    /// <summary>Includes the prefix itself and every descendant. Zip paths keep the path under the folder.</summary>
    public async Task<FileStorageArchive> ZipFolderAsync(string pathPrefix, string? fileName = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(pathPrefix);
        var rows = await _metadata.ListByPathPrefixAsync(pathPrefix, true, 10_000, ct).ConfigureAwait(false);
        var entries = new List<FileStorageArchiveEntry>(rows.Count);
        foreach (var row in rows) {
            var relative = Relative(pathPrefix, row);
            entries.Add(new(row.Id, relative));
        }

        return await _archives.CreateArchiveAsync(entries, fileName, ct).ConfigureAwait(false);
    }

    private static string Relative(string pathPrefix, FileStoreResult row)
    {
        var prefix = pathPrefix.Trim().Trim('/');
        var path = row.PathPrefix?.Trim().Trim('/') ?? "";
        var name = string.IsNullOrWhiteSpace(row.OriginalFileName) ? row.Id.ToString("N") : row.OriginalFileName!;
        if (path.Length == 0 || path.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            return name;
        if (path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            path = path.Substring(prefix.Length + 1);
        return path + "/" + name;
    }
}
