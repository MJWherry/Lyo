namespace Lyo.FileSystemWatcher;

/// <summary>Raised once per debounce after a snapshot scan, with the previous tree, current tree, and every detected change (including directory-content events).</summary>
public sealed class FileSystemScanCompletedEventArgs : EventArgs
{
    /// <summary>Snapshot taken before this scan.</summary>
    public SnapshotTree Previous { get; }

    /// <summary>Snapshot taken for this scan.</summary>
    public SnapshotTree Current { get; }

    /// <summary>Every change detected between <see cref="Previous" /> and <see cref="Current" />.</summary>
    public IReadOnlyList<FileSystemChangeInfo> Changes { get; }

    /// <summary>Builds scan-completed args. Callers should not construct this type.</summary>
    public FileSystemScanCompletedEventArgs(SnapshotTree previous, SnapshotTree current, IReadOnlyList<FileSystemChangeInfo> changes)
    {
        Previous = previous;
        Current = current;
        Changes = changes;
    }
}
