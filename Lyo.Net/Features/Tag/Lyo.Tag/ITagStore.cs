using Lyo.EntityReference.Models;

namespace Lyo.Tag;

/// <summary>Interface for storing and retrieving tags across entities.</summary>
/// <remarks>
/// Stores accept <see cref="EntityRef" /> at the API boundary but persist <c>EntityId</c> as a single Guid per Option A. Pass null for <c>tenantId</c> on methods in
/// single-tenant deployments (resolved via <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface ITagStore
{
    /// <summary>Adds a tag to an entity. Idempotent if the same tag, type, and slug already exists.</summary>
    Task AddTagAsync(
        EntityRef forEntity,
        string tag,
        string tagType = "tag",
        EntityRef? fromEntity = null,
        string? slug = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Removes a tag from an entity.</summary>
    /// <param name="slug">Must match the stored slug for that assignment (empty when none was stored).</param>
    Task RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all tags for an entity, optionally filtered by tag type.</summary>
    Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all entities with a given tag, optionally filtered by entity type and tag type.</summary>
    Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all distinct tag values that have been applied to any entity of the given type, optionally filtered by tag type.</summary>
    Task<IReadOnlyList<string>> GetAllTagsForEntityTypeAsync(string forEntityType, string? tagType = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes all tags from an entity.</summary>
    Task RemoveAllTagsForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}