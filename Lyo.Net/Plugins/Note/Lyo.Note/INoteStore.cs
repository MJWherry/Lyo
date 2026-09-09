using Lyo.EntityReference.Models;

namespace Lyo.Note;

/// <summary>Store contract for notes.</summary>
/// <remarks>
/// Stores take <see cref="EntityRef" /> at the API edge and persist <c>EntityId</c> as one Guid (Option A). Pass null for <c>tenantId</c> on methods in
/// single-tenant hosts (resolved through <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface INoteStore
{
    /// <summary>Upserts a note.</summary>
    Task SaveAsync(NoteRecord note, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Looks up a note by id.</summary>
    Task<NoteRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads all notes for an entity (what the note is about).</summary>
    Task<IReadOnlyList<NoteRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists all notes created by an entity (e.g. all notes from user 123).</summary>
    Task<IReadOnlyList<NoteRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns every notes for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<NoteRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a note by id.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes every notes for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}