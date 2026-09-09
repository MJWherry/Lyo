namespace Lyo.FileSystemWatcher.Models;

/// <summary>Persists watches, snapshots, and change batches produced by a file-system watcher.</summary>
public interface IFileSystemWatcherStore
{
    /// <summary>Inserts a watch row and returns its id.</summary>
    Task<Guid> CreateWatchAsync(string rootPath, FileSystemWatchOptionsDto options, CancellationToken ct = default);

    /// <summary>Stores a structure snapshot. When <paramref name="tree" /> matches the latest content hash for the watch, returns that existing id.</summary>
    Task<Guid> SaveSnapshotAsync(Guid watchId, FileSystemSnapshotTreeDto tree, DateTime takenAtUtc, CancellationToken ct = default);

    /// <summary>Stores a batch of change events linked to an optional snapshot.</summary>
    Task SaveChangesAsync(Guid watchId, Guid? snapshotId, IReadOnlyList<FileSystemChangeDto> changes, CancellationToken ct = default);

    /// <summary>Returns the latest snapshot tree for a watch, or null when none exist.</summary>
    Task<FileSystemSnapshotTreeDto?> GetLatestSnapshotAsync(Guid watchId, CancellationToken ct = default);

    /// <summary>Returns change events for a watch, newest first.</summary>
    Task<IReadOnlyList<FileSystemChangeDto>> GetChangesAsync(Guid watchId, int take = 100, CancellationToken ct = default);
}
