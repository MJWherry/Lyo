using Lyo.Common;

namespace Lyo.Tag;

/// <summary>Interface for storing and retrieving tags across entities.</summary>
public interface ITagStore
{
    /// <summary>Adds a tag to an entity. Idempotent if tag already exists.</summary>
    /// <param name="forEntity">The entity being tagged</param>
    /// <param name="tag">The tag value</param>
    /// <param name="fromEntity">Optional: who applied the tag (for audit)</param>
    Task AddTagAsync(EntityRef forEntity, string tag, EntityRef? fromEntity = null, CancellationToken ct = default);

    /// <summary>Removes a tag from an entity.</summary>
    Task RemoveTagAsync(EntityRef forEntity, string tag, CancellationToken ct = default);

    /// <summary>Gets all tags for an entity.</summary>
    Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Gets all entities with a given tag, optionally filtered by entity type.</summary>
    Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, CancellationToken ct = default);

    /// <summary>Removes all tags from an entity.</summary>
    Task RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}