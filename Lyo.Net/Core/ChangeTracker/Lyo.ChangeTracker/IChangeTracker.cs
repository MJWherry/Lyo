using Lyo.EntityReference.Models;

namespace Lyo.ChangeTracker;

/// <summary>Writes and reads generic entity change history.</summary>
public interface IChangeTracker
{
    /// <summary>Persists one change.</summary>
    void RecordChange(ChangeRecord change);

    /// <summary>Persists one change without blocking the caller.</summary>
    Task RecordChangeAsync(ChangeRecord change, CancellationToken ct = default);

    /// <summary>Persists several changes.</summary>
    void RecordChanges(IEnumerable<ChangeRecord> changes);

    /// <summary>Persists several changes without blocking the caller.</summary>
    Task RecordChangesAsync(IEnumerable<ChangeRecord> changes, CancellationToken ct = default);

    /// <summary>Loads a recorded change by id.</summary>
    Task<ChangeRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads change history for one entity, newest first.</summary>
    Task<IReadOnlyList<ChangeRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Loads change history for an entity type and optional entity id, newest first.</summary>
    Task<IReadOnlyList<ChangeRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default);

    /// <summary>Removes every tracked change for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}