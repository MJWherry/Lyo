using Lyo.EntityReference.Models;

namespace Lyo.Tag;

/// <summary>Interface for storing and retrieving tags across entities.</summary>
public interface ITagStore
{
    /// <summary>Adds a tag to an entity. Idempotent if the same tag, type, and slug already exists.</summary>
    /// <param name="forEntity">The entity being tagged</param>
    /// <param name="tag">The tag value</param>
    /// <param name="tagType">The tag type (e.g. "tag", "category"). Defaults to "tag".</param>
    /// <param name="fromEntity">Optional: who applied the tag (for audit)</param>
    /// <param name="slug">Optional URL-friendly slug; normalized to empty when null or whitespace.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddTagAsync(EntityRef forEntity, string tag, string tagType = "tag", EntityRef? fromEntity = null, string? slug = null, CancellationToken ct = default);

    /// <summary>Removes a tag from an entity.</summary>
    /// <param name="slug">Must match the stored slug for that assignment (empty when none was stored).</param>
    Task RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default);

    /// <summary>Gets all tags for an entity, optionally filtered by tag type.</summary>
    Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, CancellationToken ct = default);

    /// <summary>Gets all entities with a given tag, optionally filtered by entity type and tag type.</summary>
    Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, CancellationToken ct = default);

    /// <summary>Gets all distinct tag values that have been applied to any entity of the given type, optionally filtered by tag type.</summary>
    Task<IReadOnlyList<string>> GetAllTagsForEntityTypeAsync(string forEntityType, string? tagType = null, CancellationToken ct = default);

    /// <summary>Removes all tags from an entity.</summary>
    Task RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}