namespace Lyo.FileStorage;

/// <summary>One row from <see cref="FileStorageReconcile" />.</summary>
/// <param name="Presence">Store, physical, or both.</param>
/// <param name="FileId">File id from metadata or parsed from the object-key leaf.</param>
/// <param name="LogicalPath">Catalog path <c>/{prefix}/{fileId}</c> when known.</param>
/// <param name="PhysicalKey">Object key relative to the physical root, when known.</param>
/// <param name="OriginalFileName">Display name from metadata when known.</param>
public sealed record FileStorageReconcileEntry(
    FileStoragePresence Presence,
    Guid? FileId,
    string? LogicalPath,
    string? PhysicalKey,
    string? OriginalFileName);
