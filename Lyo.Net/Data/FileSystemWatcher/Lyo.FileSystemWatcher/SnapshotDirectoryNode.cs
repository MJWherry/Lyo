namespace Lyo.FileSystemWatcher;

/// <summary>One directory in a snapshot tree. Files live in <see cref="Files"/>; subdirectories in <see cref="Directories"/>.</summary>
public sealed class SnapshotDirectoryNode
{
    public SnapshotDirectoryNode(SnapshotDirectoryNode? parent, string name, string fullPath, IEqualityComparer<string> segmentComparer)
    {
        Parent = parent;
        Name = name;
        FullPath = fullPath;
        Directories = new(segmentComparer);
        Files = new(segmentComparer);
    }

    public SnapshotDirectoryNode? Parent { get; }

    /// <summary>Single path segment for this directory; empty for the snapshot root.</summary>
    public string Name { get; }

    public string FullPath { get; }

    public Dictionary<string, SnapshotDirectoryNode> Directories { get; }

    public Dictionary<string, DirectorySnapshotEntry> Files { get; }
}
