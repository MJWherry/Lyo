namespace Lyo.FileSystemWatcher;

/// <summary>One directory in a snapshot tree. Files sit in <see cref="Files" />; child directories sit in <see cref="Directories" />.</summary>
public sealed class SnapshotDirectoryNode
{
    /// <summary>Parent directory node. Null for the synthetic root under the watched path.</summary>
    public SnapshotDirectoryNode? Parent { get; }

    /// <summary>One path segment for this directory. Empty at the snapshot root.</summary>
    public string Name { get; }

    /// <summary>Absolute path of this directory.</summary>
    public string FullPath { get; }

    /// <summary>Immediate child directories keyed by segment name.</summary>
    public Dictionary<string, SnapshotDirectoryNode> Directories { get; }

    /// <summary>Files in this directory keyed by file name.</summary>
    public Dictionary<string, DirectorySnapshotEntry> Files { get; }

    /// <summary>
    /// Snapshot entry for this directory itself, built on first use and reused afterwards. Diffing walks every directory on every poll, and building a fresh
    /// <see cref="DirectoryInfo" /> each time dominated the per-poll allocation.
    /// </summary>
    public DirectorySnapshotEntry Entry => _entry ??= new(FullPath, new DirectoryInfo(FullPath));

    private DirectorySnapshotEntry? _entry;

    /// <summary>Builds a node in the snapshot tree.</summary>
    /// <param name="parent">Parent node, or null at the root.</param>
    /// <param name="name">Directory segment name. Empty at the root.</param>
    /// <param name="fullPath">Absolute path of this directory.</param>
    /// <param name="segmentComparer">Comparer for child dictionary keys (ordinal or ordinal ignore-case).</param>
    public SnapshotDirectoryNode(SnapshotDirectoryNode? parent, string name, string fullPath, IEqualityComparer<string> segmentComparer)
    {
        Parent = parent;
        Name = name;
        FullPath = fullPath;
        Directories = new(segmentComparer);
        Files = new(segmentComparer);
    }
}