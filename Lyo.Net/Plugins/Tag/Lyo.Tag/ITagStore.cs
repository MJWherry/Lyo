using Lyo.EntityReference.Models;

namespace Lyo.Tag;

/// <summary>Store contract for tags across entities.</summary>
/// <remarks>
/// Stores take <see cref="EntityRef" /> at the API edge and persist <c>EntityId</c> as one Guid (Option A). Pass null for <c>tenantId</c> on methods in
/// single-tenant hosts (resolved through <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface ITagStore
{
    /// <summary>Registers a tag to an entity. Idempotent if the same tag, type, and slug already exists.</summary>
    Task AddTagAsync(
        EntityRef forEntity,
        string tag,
        string tagType = "tag",
        EntityRef? fromEntity = null,
        string? slug = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Drops a tag from an entity.</summary>
    /// <param name="forEntity">Entity that loses the tag.</param>
    /// <param name="tag">Tag string being removed.</param>
    /// <param name="tagType">Tag category; defaults to "tag".</param>
    /// <param name="slug">Must equal the stored slug, or empty when none was stored.</param>
    /// <param name="tenantId">Tenant scope; null on single-tenant hosts.</param>
    /// <param name="ct">Token that can abort the call.</param>
    Task RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads all tags for an entity, optionally filtered by tag type.</summary>
    Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns every entities with a given tag, optionally filtered by entity type and tag type.</summary>
    Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads all distinct tag values that have been applied to any entity of the given type, optionally filtered by tag type.</summary>
    Task<IReadOnlyList<string>> GetAllTagsForEntityTypeAsync(string forEntityType, string? tagType = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Clears all tags from an entity.</summary>
    Task RemoveAllTagsForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}