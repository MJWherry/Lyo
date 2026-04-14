using Lyo.Common;

namespace Lyo.ChangeTracker;

/// <summary>Records and retrieves generic entity change history.</summary>
public interface IChangeTracker
{
    /// <summary>Records a single change.</summary>
    void RecordChange(ChangeRecord change);

    /// <summary>Records a single change asynchronously.</summary>
    Task RecordChangeAsync(ChangeRecord change, CancellationToken ct = default);

    /// <summary>Records multiple changes.</summary>
    void RecordChanges(IEnumerable<ChangeRecord> changes);

    /// <summary>Records multiple changes asynchronously.</summary>
    Task RecordChangesAsync(IEnumerable<ChangeRecord> changes, CancellationToken ct = default);

    /// <summary>Gets a recorded change by id.</summary>
    Task<ChangeRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets change history for a single entity ordered newest first.</summary>
    Task<IReadOnlyList<ChangeRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Gets change history for an entity type and optional entity id ordered newest first.</summary>
    Task<IReadOnlyList<ChangeRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default);

    /// <summary>Deletes all tracked changes for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}