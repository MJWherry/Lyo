using Lyo.FileSystemWatcher.Models;
using Lyo.Exceptions;

namespace Lyo.FileSystemWatcher;

/// <summary>Maps live watcher types onto persistable DTOs. Copies timestamps and size off <c>FileSystemInfo</c> immediately.</summary>
public static class FileSystemWatcherMapping
{
    /// <summary>Maps watch options to a serializable DTO.</summary>
    public static FileSystemWatchOptionsDto ToDto(this FileSystemWatcherOptions options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        return new() {
            IncludeSubdirectories = options.IncludeSubdirectories,
            DebounceTimerDelay = options.DebounceTimerDelay,
            EnableFileHashing = options.EnableFileHashing,
            PathComparison = options.PathComparison.ToString(),
            IncludePatterns = options.IncludePatterns.ToList(),
            ExcludePatterns = options.ExcludePatterns.ToList()
        };
    }

    /// <summary>Maps a live change to a DTO with paths relative to <paramref name="rootPath" />.</summary>
    public static FileSystemChangeDto ToDto(this FileSystemChangeInfo change, string rootPath, DateTime? occurredAtUtc = null)
    {
        ArgumentHelpers.ThrowIfNull(change);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        return new() {
            OldPath = change.OldPath is null ? null : ToRelativePath(rootPath, change.OldPath, StringComparison.OrdinalIgnoreCase),
            NewPath = change.NewPath is null ? null : ToRelativePath(rootPath, change.NewPath, StringComparison.OrdinalIgnoreCase),
            ChangeType = (FileSystemChangeKind)(int)change.ChangeType,
            IsDirectory = change.IsDirectory,
            OldFileCount = change.OldFileCount,
            OldDirectoryCount = change.OldDirectoryCount,
            NewFileCount = change.NewFileCount,
            NewDirCount = change.NewDirCount,
            OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow
        };
    }

    /// <summary>Maps a live snapshot tree to a persistable DTO. Paths are relative to the snapshot root.</summary>
    public static FileSystemSnapshotTreeDto ToDto(this SnapshotTree tree)
    {
        ArgumentHelpers.ThrowIfNull(tree);
        var dto = new FileSystemSnapshotTreeDto {
            RootPath = tree.RootPath,
            PathComparison = tree.PathComparison.ToString(),
            FileCount = tree.FileCount,
            DirectoryCount = tree.DirectoryCount,
            Root = MapDirectory(tree.Root, tree.RootPath, tree.PathComparison, tree.SegmentComparer)
        };
        return dto;
    }

    /// <summary>Turns an absolute path into a '/' relative path under <paramref name="rootPath" />.</summary>
    public static string ToRelativePath(string rootPath, string fullPath, StringComparison comparison)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentHelpers.ThrowIfNull(fullPath);
        var r = SnapshotTree.TrimSeparators(rootPath);
        var f = SnapshotTree.TrimSeparators(fullPath);
        if (string.Equals(r, f, comparison))
            return "";

        if (f.Length > r.Length && f.StartsWith(r, comparison)
            && (f[r.Length] == Path.DirectorySeparatorChar || f[r.Length] == Path.AltDirectorySeparatorChar))
            return FileSystemPathGlob.Normalize(f.Substring(r.Length));

        return FileSystemPathGlob.Normalize(f);
    }

    private static FileSystemSnapshotDirectoryDto MapDirectory(
        SnapshotDirectoryNode node,
        string rootPath,
        StringComparison comparison,
        StringComparer segmentComparer)
    {
        var dto = new FileSystemSnapshotDirectoryDto {
            RelativePath = ToRelativePath(rootPath, node.FullPath, comparison),
            Directories = new(segmentComparer),
            Files = new(segmentComparer)
        };
        foreach (var pair in node.Directories)
            dto.Directories[pair.Key] = MapDirectory(pair.Value, rootPath, comparison, segmentComparer);

        foreach (var pair in node.Files)
            dto.Files[pair.Key] = MapFile(pair.Value, rootPath, comparison);

        return dto;
    }

    private static FileSystemSnapshotEntryDto MapFile(DirectorySnapshotEntry entry, string rootPath, StringComparison comparison)
    {
        DateTime? lastWrite = null;
        DateTime? created = null;
        int? attributes = null;
        long? size = entry.FileSize;
        try {
            lastWrite = entry.Info.LastWriteTimeUtc;
            created = entry.Info.CreationTimeUtc;
            attributes = (int)entry.Info.Attributes;
            if (size is null && entry.Info is FileInfo fileInfo)
                size = fileInfo.Length;
        }
        catch (IOException) {
            // Info can go stale; keep whatever was already copied onto the entry.
        }
        catch (UnauthorizedAccessException) { }

        return new() {
            Path = ToRelativePath(rootPath, entry.Path, comparison),
            IsDirectory = entry.Info is DirectoryInfo,
            FileSize = size,
            LastWriteTimeUtc = lastWrite,
            CreationTimeUtc = created,
            Attributes = attributes,
            Hash = entry.Hash,
            Fingerprint = entry.Fingerprint
        };
    }
}
