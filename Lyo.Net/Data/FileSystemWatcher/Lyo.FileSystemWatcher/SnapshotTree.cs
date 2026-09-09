namespace Lyo.FileSystemWatcher;

/// <summary>
/// Hierarchical snapshot of a directory. One <see cref="SnapshotDirectoryNode" /> per subdirectory, files grouped under their parent. Avoids a flat map keyed by full
/// path.
/// </summary>
public sealed class SnapshotTree
{
    /// <summary>Root directory this snapshot was taken from.</summary>
    public string RootPath { get; }

    /// <summary>How paths are compared when looking them up in this tree.</summary>
    public StringComparison PathComparison { get; }

    /// <summary>Comparer for path-segment keys, derived from <see cref="PathComparison" />.</summary>
    public StringComparer SegmentComparer { get; }

    /// <summary>Synthetic root that holds the top-level children of <see cref="RootPath" />.</summary>
    public SnapshotDirectoryNode Root { get; }

    /// <summary>How many file entries the tree holds.</summary>
    public int FileCount { get; }

    /// <summary>How many directory nodes exist, not counting the synthetic root when it applies.</summary>
    public int DirectoryCount { get; }

    /// <summary>Files plus directories, excluding the snapshot root node. Matches the old flat-dictionary key count.</summary>
    public int TotalEntryCount => FileCount + DirectoryCount;

    /// <summary>Builds a snapshot descriptor. Prefer <see cref="Utilities.TakeSnapshot" /> to construct one.</summary>
    public SnapshotTree(string rootPath, StringComparison pathComparison, SnapshotDirectoryNode root, int fileCount, int directoryCount)
    {
        RootPath = rootPath;
        PathComparison = pathComparison;
        SegmentComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        Root = root;
        FileCount = fileCount;
        DirectoryCount = directoryCount;
    }

    /// <summary>True if <paramref name="fullPath" /> is a file or directory node in this snapshot.</summary>
    public bool ContainsPath(string fullPath)
    {
        if (TryGetDirectory(fullPath, out var _))
            return true;

        return TryGetFile(fullPath, out var _);
    }

    /// <summary>Looks up a file path and returns its <see cref="DirectorySnapshotEntry" />.</summary>
    public bool TryGetFile(string fullPath, out DirectorySnapshotEntry? entry)
    {
        entry = null;
        var parent = Path.GetDirectoryName(fullPath);
        var fileName = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(parent))
            return false;

        if (!TryGetDirectory(parent, out var dirNode) || dirNode is null)
            return false;

        return dirNode.Files.TryGetValue(fileName, out entry);
    }

    /// <summary>Looks up a directory path and returns its <see cref="SnapshotDirectoryNode" />.</summary>
    public bool TryGetDirectory(string fullPath, out SnapshotDirectoryNode? node)
    {
        node = null;
        if (!IsUnderRoot(RootPath, fullPath, PathComparison))
            return false;

        if (PathsEqual(RootPath, fullPath, PathComparison)) {
            node = Root;
            return true;
        }

        if (!TryGetSegmentsBelowRoot(RootPath, fullPath, PathComparison, out var segments))
            return false;

        var current = Root;
        foreach (var segment in segments) {
            if (!current.Directories.TryGetValue(segment, out var next))
                return false;

            current = next;
        }

        node = current;
        return true;
    }

    /// <summary>Every file in the tree as (full path, entry), depth-first.</summary>
    public IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateFiles()
    {
        foreach (var pair in EnumerateFiles(Root))
            yield return pair;
    }

    private static IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateFiles(SnapshotDirectoryNode dir)
    {
        foreach (var entry in dir.Files.Values)
            yield return (entry.Path, entry);

        foreach (var sub in dir.Directories.Values) {
            foreach (var pair in EnumerateFiles(sub))
                yield return pair;
        }
    }

    /// <summary>Every directory path except the snapshot root, plus every file path. Used to detect deletes.</summary>
    public IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateDirectoryAndFileEntries()
    {
        foreach (var pair in EnumerateDirectoryAndFileEntries(Root))
            yield return pair;
    }

    private IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateDirectoryAndFileEntries(SnapshotDirectoryNode dir)
    {
        foreach (var sub in dir.Directories.Values) {
            yield return (sub.FullPath, new(sub.FullPath, new DirectoryInfo(sub.FullPath)));

            foreach (var pair in EnumerateDirectoryAndFileEntries(sub))
                yield return pair;
        }

        foreach (var entry in dir.Files.Values)
            yield return (entry.Path, entry);
    }

    public IEnumerable<SnapshotDirectoryNode> EnumerateDirectoryNodes()
    {
        foreach (var sub in Root.Directories.Values) {
            yield return sub;

            foreach (var n in EnumerateDirectoryNodes(sub))
                yield return n;
        }
    }

    private static IEnumerable<SnapshotDirectoryNode> EnumerateDirectoryNodes(SnapshotDirectoryNode dir)
    {
        foreach (var sub in dir.Directories.Values) {
            yield return sub;

            foreach (var n in EnumerateDirectoryNodes(sub))
                yield return n;
        }
    }

    internal static bool IsUnderRoot(string rootPath, string fullPath, StringComparison comparison)
    {
        var r = TrimSeparators(rootPath);
        var f = TrimSeparators(fullPath);
        if (f.Length < r.Length)
            return false;

        if (!f.StartsWith(r, comparison))
            return false;

        return f.Length == r.Length || f[r.Length] == Path.DirectorySeparatorChar || f[r.Length] == Path.AltDirectorySeparatorChar;
    }

    internal static bool PathsEqual(string a, string b, StringComparison comparison) => string.Equals(TrimSeparators(a), TrimSeparators(b), comparison);

    internal static string TrimSeparators(string path) => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>Returns path segments from the first segment under root through the target (directory or file-name segments).</summary>
    internal static bool TryGetSegmentsBelowRoot(string rootPath, string fullPath, StringComparison comparison, out string[] segments)
    {
        segments = Array.Empty<string>();
        var r = TrimSeparators(rootPath);
        var f = TrimSeparators(fullPath);
        if (f.Length < r.Length + 1 || !f.StartsWith(r, comparison))
            return false;

        if (f.Length > r.Length && f[r.Length] != Path.DirectorySeparatorChar && f[r.Length] != Path.AltDirectorySeparatorChar)
            return false;

        var remainder = f.Substring(r.Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (remainder.Length == 0) {
            segments = Array.Empty<string>();
            return true;
        }

        segments = remainder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        return true;
    }
}