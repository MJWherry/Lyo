namespace Lyo.FileSystemWatcher;

/// <summary>
/// Hierarchical snapshot of a directory: one <see cref="SnapshotDirectoryNode"/> per subdirectory, with files grouped by parent directory. Avoids storing a flat map keyed by full path.
/// </summary>
public sealed class SnapshotTree
{
    public SnapshotTree(string rootPath, StringComparison pathComparison, SnapshotDirectoryNode root, int fileCount, int directoryCount)
    {
        RootPath = rootPath;
        PathComparison = pathComparison;
        SegmentComparer = pathComparison == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        Root = root;
        FileCount = fileCount;
        DirectoryCount = directoryCount;
    }

    public string RootPath { get; }

    public StringComparison PathComparison { get; }

    public StringComparer SegmentComparer { get; }

    public SnapshotDirectoryNode Root { get; }

    public int FileCount { get; }

    public int DirectoryCount { get; }

    /// <summary>Files plus directories (excluding the snapshot root node), matching prior flat dictionary key count.</summary>
    public int TotalEntryCount => FileCount + DirectoryCount;

    public bool ContainsPath(string fullPath)
    {
        if (TryGetDirectory(fullPath, out _))
            return true;

        return TryGetFile(fullPath, out _);
    }

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

    /// <summary>All files in the tree (full path + entry), depth-first.</summary>
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

    /// <summary>Every directory path (except the snapshot root) and file path, for delete detection.</summary>
    public IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateDirectoryAndFileEntries()
    {
        foreach (var pair in EnumerateDirectoryAndFileEntries(Root))
            yield return pair;
    }

    private IEnumerable<(string Path, DirectorySnapshotEntry Entry)> EnumerateDirectoryAndFileEntries(SnapshotDirectoryNode dir)
    {
        foreach (var sub in dir.Directories.Values) {
            yield return (sub.FullPath, new DirectorySnapshotEntry(sub.FullPath, new DirectoryInfo(sub.FullPath)));
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

        return f.Length == r.Length
            || f[r.Length] == Path.DirectorySeparatorChar
            || f[r.Length] == Path.AltDirectorySeparatorChar;
    }

    internal static bool PathsEqual(string a, string b, StringComparison comparison)
        => string.Equals(TrimSeparators(a), TrimSeparators(b), comparison);

    internal static string TrimSeparators(string path)
        => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>Returns path segments from the first segment below root to the target (directory or file name segments).</summary>
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

        segments = remainder.Split(
            new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
            StringSplitOptions.RemoveEmptyEntries);

        return true;
    }
}
