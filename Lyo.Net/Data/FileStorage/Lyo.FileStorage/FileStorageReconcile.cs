using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage;

/// <summary>Joins metadata rows to a physical <see cref="IFileSystem" /> list by file id parsed from the object-key leaf.</summary>
public static class FileStorageReconcile
{
    /// <summary>
    /// Walks metadata under <paramref name="pathPrefix" /> and files under the matching physical directory.
    /// Health probes, local metadata sidecars, and temp leftovers are skipped.
    /// Tombstoned metadata is omitted from the store listing, so a leftover object is <see cref="FileStoragePresence.Physical" />.
    /// </summary>
    public static async Task<IReadOnlyList<FileStorageReconcileEntry>> ReconcileAsync(
        IFileMetadataStore metadata,
        IFileSystem physical,
        string? storagePrefix = null,
        string? pathPrefix = null,
        int maxKeys = 10_000,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(metadata);
        ArgumentHelpers.ThrowIfNull(physical);
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var rows = await metadata.ListByPathPrefixAsync(pathPrefix, true, maxKeys, ct).ConfigureAwait(false);
        var start = PhysicalDirectory(physical, pathPrefix);
        var physicalFiles = await FlattenFilesAsync(physical, start, maxKeys, ct).ConfigureAwait(false);
        var physicalById = new Dictionary<Guid, string>();
        foreach (var key in physicalFiles) {
            if (IsSkippedPhysicalKey(key))
                continue;

            if (TryParseFileIdFromKey(key, out var id))
                physicalById[id] = key;
        }

        List<FileStorageReconcileEntry> result = [];
        var matched = new HashSet<Guid>();
        foreach (var row in rows) {
            var expected = CloudObjectKeyBuilder.FromMetadata(row.Id, row.SourceFileName, row.PathPrefix, storagePrefix);
            var hit = physicalById.TryGetValue(row.Id, out var listed) || physicalFiles.Contains(expected);
            if (hit)
                matched.Add(row.Id);

            result.Add(
                new(
                    hit ? FileStoragePresence.Both : FileStoragePresence.Store, row.Id, FileStorageLogicalPath.For(row.PathPrefix, row.Id),
                    hit ? listed ?? expected : expected, row.OriginalFileName));
        }

        foreach (var pair in physicalById) {
            if (matched.Contains(pair.Key))
                continue;

            result.Add(new(FileStoragePresence.Physical, pair.Key, null, pair.Value, null));
        }

        foreach (var key in physicalFiles) {
            if (IsSkippedPhysicalKey(key) || TryParseFileIdFromKey(key, out _))
                continue;

            result.Add(new(FileStoragePresence.Physical, null, null, key, null));
        }

        return result;
    }

    /// <summary>Parses a stored object key leaf <c>{fileId:N}{suffix}</c>.</summary>
    public static bool TryParseFileIdFromKey(string key, out Guid fileId)
    {
        fileId = default;
        if (string.IsNullOrWhiteSpace(key) || IsSkippedPhysicalKey(key))
            return false;

        var parts = key.Replace('\\', '/').Split('/');
        var last = parts[parts.Length - 1];
        if (last.Length < 32)
            return false;

        return Guid.TryParseExact(last.Substring(0, 32), "N", out fileId);
    }

    /// <summary>True for health probes, <c>.meta</c> sidecars, and temp leftovers that are not catalog files.</summary>
    public static bool IsSkippedPhysicalKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        var normalized = key.Replace('\\', '/');
        if (normalized.IndexOf(".lyo-health", StringComparison.Ordinal) >= 0
            || normalized.IndexOf(".lyo-fs-health", StringComparison.Ordinal) >= 0)
            return true;

        if (normalized.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            return true;

        var last = PathHelpersLastSegment(normalized);
        return last.StartsWith(".partial-", StringComparison.Ordinal)
               || last.IndexOf(".partial-", StringComparison.Ordinal) >= 0
               || last.StartsWith(".dek-rotate-", StringComparison.Ordinal)
               || last.IndexOf(".dek-rotate-", StringComparison.Ordinal) >= 0;
    }

    /// <summary>Physical directory for a catalog PathPrefix, using the backend's <see cref="IFileSystem.PathStyle" />.</summary>
    public static string PhysicalDirectory(IFileSystem physical, string? pathPrefix)
    {
        ArgumentHelpers.ThrowIfNull(physical);
        var prefix = FileMetadataPathPrefix.Normalize(pathPrefix);
        if (prefix == null)
            return physical.RootPath;

        if (physical.PathStyle == Common.Core.Pathing.PathStyle.Posix) {
            var root = physical.RootPath.Replace('\\', '/').TrimEnd('/');
            if (root.Length == 0)
                root = "/";
            return root == "/" ? "/" + prefix : root + "/" + prefix;
        }

        return Path.Combine(physical.RootPath, prefix.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string PathHelpersLastSegment(string posixKey)
    {
        var slash = posixKey.LastIndexOf('/');
        return slash < 0 ? posixKey : posixKey.Substring(slash + 1);
    }

    private static async Task<List<string>> FlattenFilesAsync(IFileSystem fs, string path, int maxKeys, CancellationToken ct)
    {
        List<string> keys = [];
        await FlattenIntoAsync(fs, path, keys, maxKeys, ct).ConfigureAwait(false);
        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    private static async Task FlattenIntoAsync(IFileSystem fs, string path, List<string> keys, int maxKeys, CancellationToken ct)
    {
        if (keys.Count >= maxKeys)
            return;

        IReadOnlyList<FileSystemEntry> entries;
        try {
            entries = await fs.ListDirectoryAsync(path, ct).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException) {
            return;
        }
        catch (IOException) {
            return;
        }

        foreach (var entry in entries.OrderBy(static e => e.Path, StringComparer.Ordinal)) {
            if (keys.Count >= maxKeys)
                return;

            if (entry.IsDirectory)
                await FlattenIntoAsync(fs, entry.Path, keys, maxKeys, ct).ConfigureAwait(false);
            else
                keys.Add(RelativeKey(physicalRoot: fs.RootPath, entry.Path));
        }
    }

    private static string RelativeKey(string physicalRoot, string fullPath)
    {
        var full = fullPath.Replace('\\', '/').TrimEnd('/');
        var root = physicalRoot.Replace('\\', '/').TrimEnd('/');
        if (root == "")
            root = "/";

        if (full.Length >= root.Length && (full.Equals(root, StringComparison.OrdinalIgnoreCase) || full.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)))
            return full.Length == root.Length ? "" : full.Substring(root.Length + 1);

        return ObjectStoreVfs.ToObjectKey(fullPath);
    }
}
