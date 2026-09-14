using System.Linq;
using Lyo.Exceptions;
using Lyo.FileMetadataStore;
using Lyo.FileStorage.Abstractions;
using Lyo.IO.FileSystem;

namespace Lyo.FileStorage;

/// <summary>One immediate child of a catalog folder after joining store rows to physical keys.</summary>
/// <param name="Name">Folder segment or original file name.</param>
/// <param name="IsDirectory">True for a PathPrefix folder.</param>
/// <param name="PathPrefix">Folder prefix, or the file's PathPrefix.</param>
/// <param name="FileId">Metadata / parsed physical file id.</param>
/// <param name="Presence">Store, physical, or both.</param>
/// <param name="OriginalFileName">Display name from metadata when known.</param>
/// <param name="OriginalFileSize">Size from metadata when known.</param>
/// <param name="PhysicalKey">Object key relative to the physical root, when known.</param>
public sealed record FileStorageFolderEntry(
    string Name,
    bool IsDirectory,
    string? PathPrefix,
    Guid? FileId,
    FileStoragePresence Presence,
    string? OriginalFileName,
    long OriginalFileSize,
    string? PhysicalKey);

/// <summary>Immediate children of one PathPrefix: catalog files, child folders, and physical-only orphans.</summary>
public static class FileStorageFolderList
{
    /// <summary>
    /// Lists one folder. When <paramref name="physical" /> is null, catalog files are <see cref="FileStoragePresence.Store" />. When physical listing ran and a row did
    /// not join, presence is <see cref="FileStoragePresence.None" />.
    /// </summary>
    public static async Task<(IReadOnlyList<FileStorageFolderEntry> Entries, bool Truncated)> ListAsync(
        IFileMetadataStore store,
        IFileSystem? physical,
        string? pathPrefix,
        int maxKeys = 10_000,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(store);
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        var parent = FileMetadataPathPrefix.Normalize(pathPrefix);
        var immediate = await store.ListByPathPrefixAsync(parent, false, maxKeys, ct).ConfigureAwait(false);
        var descendants = await store.ListByPathPrefixAsync(parent, true, maxKeys, ct).ConfigureAwait(false);
        var truncated = immediate.Count >= maxKeys || descendants.Count >= maxKeys;

        IReadOnlyList<FileStorageReconcileEntry> reconciled = [];
        if (physical != null) {
            reconciled = await FileStorageReconcile.ReconcileAsync(store, physical, pathPrefix: parent, maxKeys: maxKeys, ct: ct).ConfigureAwait(false);
            if (reconciled.Count >= maxKeys)
                truncated = true;
        }

        var presenceById = new Dictionary<Guid, FileStorageReconcileEntry>();
        foreach (var row in reconciled) {
            if (row.FileId is { } id)
                presenceById[id] = row;
        }

        List<FileStorageFolderEntry> entries = [];
        var folders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in descendants) {
            var child = FileMetadataPathPrefix.ImmediateChildFolder(parent, row.PathPrefix);
            if (child == null || !folders.Add(child))
                continue;

            var childPrefix = parent == null ? child : parent + "/" + child;
            entries.Add(new(child, true, childPrefix, null, FileStoragePresence.Store, null, 0, null));
        }

        foreach (var row in immediate) {
            presenceById.TryGetValue(row.Id, out var joined);
            var presence = physical == null
                ? FileStoragePresence.Store
                : joined?.Presence ?? FileStoragePresence.None;
            entries.Add(
                new(
                    row.OriginalFileName ?? row.Id.ToString("D"), false, row.PathPrefix, row.Id, presence, row.OriginalFileName, row.OriginalFileSize,
                    joined?.PhysicalKey));
        }

        if (physical != null) {
            foreach (var row in reconciled) {
                if (row.Presence != FileStoragePresence.Physical || row.PhysicalKey == null)
                    continue;

                var keyPrefix = PrefixFromPhysicalKey(row.PhysicalKey, row.FileId);
                if (!FileMetadataPathPrefix.IsImmediate(parent, keyPrefix)) {
                    var child = FileMetadataPathPrefix.ImmediateChildFolder(parent, keyPrefix);
                    if (child != null && folders.Add(child)) {
                        var childPrefix = parent == null ? child : parent + "/" + child;
                        entries.Add(new(child, true, childPrefix, null, FileStoragePresence.Physical, null, 0, null));
                    }

                    continue;
                }

                if (row.FileId is { } orphanId && immediate.Any(f => f.Id == orphanId))
                    continue;

                var name = row.OriginalFileName ?? PathHelpersLastSegment(row.PhysicalKey);
                entries.Add(new(name, false, parent, row.FileId, FileStoragePresence.Physical, row.OriginalFileName, 0, row.PhysicalKey));
            }
        }

        entries.Sort(static (a, b) => {
            if (a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;

            return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
        });
        return (entries, truncated);
    }

    /// <summary>
    /// Lists one folder using <paramref name="fileStorage" />'s physical backend when the service implements <see cref="IFileStoragePhysical" />. Hosts that must not
    /// reference <see cref="IFileSystem" /> should call this overload.
    /// </summary>
    public static Task<(IReadOnlyList<FileStorageFolderEntry> Entries, bool Truncated)> ListAsync(
        IFileMetadataStore store,
        IFileStorageService fileStorage,
        string? pathPrefix,
        int maxKeys = 10_000,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fileStorage);
        var physical = fileStorage is IFileStoragePhysical listing ? listing.Physical : null;
        return ListAsync(store, physical, pathPrefix, maxKeys, ct);
    }

    private static string? PrefixFromPhysicalKey(string key, Guid? fileId)
    {
        var posix = key.Replace('\\', '/').Trim('/');
        var parts = posix.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
            return null;

        var prefixLength = parts.Length - 1;
        if (fileId is { } id) {
            var n = id.ToString("N");
            if (prefixLength == 2 && parts[0] == n.Substring(0, 2) && parts[1] == n.Substring(2, 2))
                return null;
        }

        return string.Join("/", parts, 0, prefixLength);
    }

    private static string PathHelpersLastSegment(string key)
    {
        var posix = key.Replace('\\', '/');
        var slash = posix.LastIndexOf('/');
        return slash < 0 ? posix : posix.Substring(slash + 1);
    }
}
