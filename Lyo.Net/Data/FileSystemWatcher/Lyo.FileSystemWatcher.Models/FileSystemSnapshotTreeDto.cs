using Lyo.Exceptions;

namespace Lyo.FileSystemWatcher.Models;

/// <summary>
/// Hierarchical persistable snapshot of a directory. Paths inside the tree are relative to <see cref="RootPath" /> and use '/' separators.
/// </summary>
public sealed class FileSystemSnapshotTreeDto
{
    /// <summary>Absolute watch root this snapshot was taken from.</summary>
    public string RootPath { get; set; } = "";

    /// <summary>How paths are compared. <c>Ordinal</c> or <c>OrdinalIgnoreCase</c>.</summary>
    public string PathComparison { get; set; } = nameof(StringComparison.OrdinalIgnoreCase);

    /// <summary>How many file entries the tree holds.</summary>
    public int FileCount { get; set; }

    /// <summary>How many directory nodes exist, not counting the synthetic root.</summary>
    public int DirectoryCount { get; set; }

    /// <summary>Synthetic root that holds the top-level children of <see cref="RootPath" />.</summary>
    public FileSystemSnapshotDirectoryDto Root { get; set; } = new();

    /// <summary>Resolved <see cref="PathComparison" /> value.</summary>
    public StringComparison GetPathComparison()
        => string.Equals(PathComparison, nameof(StringComparison.Ordinal), StringComparison.Ordinal)
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

    /// <summary>True if <paramref name="relativePath" /> is a file or directory node in this snapshot.</summary>
    public bool ContainsPath(string relativePath)
    {
        ArgumentHelpers.ThrowIfNull(relativePath);
        return TryGetDirectory(relativePath, out _) || TryGetFile(relativePath, out _);
    }

    /// <summary>Looks up a file by relative path.</summary>
    public bool TryGetFile(string relativePath, out FileSystemSnapshotEntryDto? entry)
    {
        entry = null;
        var normalized = FileSystemPathGlob.Normalize(relativePath);
        if (normalized.Length == 0)
            return false;

        if (!TryGetParentDirectory(normalized, out var dir) || dir is null)
            return false;

        var fileName = GetFileName(normalized);
        foreach (var pair in dir.Files) {
            if (string.Equals(pair.Key, fileName, GetPathComparison())) {
                entry = pair.Value;
                return true;
            }
        }

        return false;
    }

    /// <summary>Looks up a directory node by relative path. Empty path is the snapshot root.</summary>
    public bool TryGetDirectory(string relativePath, out FileSystemSnapshotDirectoryDto? node)
    {
        node = null;
        var normalized = FileSystemPathGlob.Normalize(relativePath);
        if (normalized.Length == 0) {
            node = Root;
            return true;
        }

        var current = Root;
        foreach (var segment in Split(normalized)) {
            FileSystemSnapshotDirectoryDto? next = null;
            foreach (var pair in current.Directories) {
                if (!string.Equals(pair.Key, segment, GetPathComparison()))
                    continue;

                next = pair.Value;
                break;
            }

            if (next is null)
                return false;

            current = next;
        }

        node = current;
        return true;
    }

    /// <summary>Every file in the tree as (relative path, entry), depth-first.</summary>
    public IEnumerable<(string Path, FileSystemSnapshotEntryDto Entry)> EnumerateFiles()
        => EnumerateFiles(Root);

    /// <summary>Every directory relative path except the snapshot root.</summary>
    public IEnumerable<(string Path, FileSystemSnapshotDirectoryDto Node)> EnumerateDirectories()
        => EnumerateDirectories(Root);

    /// <summary>Counts files and descendant directories under <paramref name="relativeDirectory" /> as stored in this tree.</summary>
    public (int FileCount, int DirectoryCount) GetSnapshotCounts(string relativeDirectory)
    {
        if (!TryGetDirectory(relativeDirectory, out var node) || node is null)
            return (0, 0);

        var files = 0;
        var dirs = 0;
        CountDescendants(node, ref files, ref dirs);
        return (files, dirs);
    }

    private bool TryGetParentDirectory(string normalizedFilePath, out FileSystemSnapshotDirectoryDto? dir)
    {
        var slash = normalizedFilePath.LastIndexOf('/');
        if (slash < 0)
            return TryGetDirectory("", out dir);

        return TryGetDirectory(normalizedFilePath.Substring(0, slash), out dir);
    }

    private static IEnumerable<(string Path, FileSystemSnapshotEntryDto Entry)> EnumerateFiles(FileSystemSnapshotDirectoryDto dir)
    {
        foreach (var entry in dir.Files.Values)
            yield return (entry.Path, entry);

        foreach (var sub in dir.Directories.Values) {
            foreach (var pair in EnumerateFiles(sub))
                yield return pair;
        }
    }

    private static IEnumerable<(string Path, FileSystemSnapshotDirectoryDto Node)> EnumerateDirectories(FileSystemSnapshotDirectoryDto dir)
    {
        foreach (var sub in dir.Directories.Values) {
            yield return (sub.RelativePath, sub);
            foreach (var pair in EnumerateDirectories(sub))
                yield return pair;
        }
    }

    private static void CountDescendants(FileSystemSnapshotDirectoryDto node, ref int fileCount, ref int dirCount)
    {
        fileCount += node.Files.Count;
        foreach (var sub in node.Directories.Values) {
            dirCount++;
            CountDescendants(sub, ref fileCount, ref dirCount);
        }
    }

    private static string GetFileName(string normalizedPath)
    {
        var slash = normalizedPath.LastIndexOf('/');
        return slash < 0 ? normalizedPath : normalizedPath.Substring(slash + 1);
    }

    private static IEnumerable<string> Split(string normalized)
        => normalized.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
}
