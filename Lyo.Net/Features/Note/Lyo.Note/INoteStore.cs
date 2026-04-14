using Lyo.Common;

namespace Lyo.Note;

/// <summary>Interface for storing and retrieving notes.</summary>
public interface INoteStore
{
    /// <summary>Adds or updates a note.</summary>
    Task SaveAsync(NoteRecord note, CancellationToken ct = default);

    /// <summary>Gets a note by id.</summary>
    Task<NoteRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets all notes for an entity (what the note is about).</summary>
    Task<IReadOnlyList<NoteRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Gets all notes created by an entity (e.g. all notes from user 123).</summary>
    Task<IReadOnlyList<NoteRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets all notes for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<NoteRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default);

    /// <summary>Deletes a note by id.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deletes all notes for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}