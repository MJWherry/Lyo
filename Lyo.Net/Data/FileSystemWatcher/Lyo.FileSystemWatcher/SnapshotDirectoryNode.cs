namespace Lyo.FileSystemWatcher;

/// <summary>One directory in a snapshot tree. Files live in <see cref="Files" />; subdirectories in <see cref="Directories" />.</summary>
public sealed class SnapshotDirectoryNode
{
    /// <summary>Parent directory node; null for the synthetic root under the watched path.</summary>
    public SnapshotDirectoryNode? Parent { get; }

    /// <summary>Single path segment for this directory; empty for the snapshot root.</summary>
    public string Name { get; }

    /// <summary>Absolute directory path.</summary>
    public string FullPath { get; }

    /// <summary>Immediate subdirectories keyed by segment name.</summary>
    public Dictionary<string, SnapshotDirectoryNode> Directories { get; }

    /// <summary>Files in this directory keyed by file name.</summary>
    public Dictionary<string, DirectorySnapshotEntry> Files { get; }

    /// <summary>Constructs a node in the snapshot tree.</summary>
    /// <param name="parent">Parent node, or null for root.</param>
    /// <param name="name">Directory segment name (empty at root).</param>
    /// <param name="fullPath">Absolute path of this directory.</param>
    /// <param name="segmentComparer">Comparer for child dictionary keys (ordinal or ordinal ignore case).</param>
    public SnapshotDirectoryNode(SnapshotDirectoryNode? parent, string name, string fullPath, IEqualityComparer<string> segmentComparer)
    {
        Parent = parent;
        Name = name;
        FullPath = fullPath;
        Directories = new(segmentComparer);
        Files = new(segmentComparer);
    }
}