using Lyo.EntityReference.Models;

namespace Lyo.Note;

/// <summary>Interface for storing and retrieving notes.</summary>
/// <remarks>
/// Stores accept <see cref="EntityRef" /> at the API boundary but persist <c>EntityId</c> as a single Guid per Option A. Pass null for <c>tenantId</c> on methods in single-tenant
/// deployments (resolved via <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface INoteStore
{
    /// <summary>Adds or updates a note.</summary>
    Task SaveAsync(NoteRecord note, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets a note by id.</summary>
    Task<NoteRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all notes for an entity (what the note is about).</summary>
    Task<IReadOnlyList<NoteRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all notes created by an entity (e.g. all notes from user 123).</summary>
    Task<IReadOnlyList<NoteRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all notes for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<NoteRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes a note by id.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes all notes for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}