using Lyo.Common.Core.Enums;
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

    /// <summary>Returns the most-used tags, each with how many active assignments use that name and tag type.</summary>
    /// <param name="take">Maximum rows to return. Must be at least 1.</param>
    /// <param name="sortBy">Count or name. Defaults to count.</param>
    /// <param name="direction">Ascending or descending. Defaults to descending.</param>
    /// <param name="forEntityType">When set, only assignments on that entity type are counted.</param>
    /// <param name="tagType">When set, only that tag type is counted. Null counts every type.</param>
    /// <param name="name">When set, only that tag name is counted. Null counts every name.</param>
    /// <param name="text">When set, only tags whose name or tag type contains this text are counted. The match is case-insensitive. Null or whitespace counts every tag.</param>
    /// <param name="skip">Rows to skip before <paramref name="take" />. Defaults to 0. Must not be negative.</param>
    /// <param name="tenantId">Tenant scope; null on single-tenant hosts.</param>
    /// <param name="ct">Token that can abort the call.</param>
    /// <remarks>
    /// Count sorts break ties by name, then tag type. Name sorts break ties by tag type. The tie-break does not follow <paramref name="direction" />.
    /// Soft-deleted rows are excluded.
    /// </remarks>
    Task<IReadOnlyList<TagCount>> GetTopTagsAsync(
        int take,
        TagSort sortBy = TagSort.Count,
        SortDirection direction = SortDirection.Desc,
        string? forEntityType = null,
        string? tagType = null,
        string? name = null,
        string? text = null,
        int skip = 0,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Clears all tags from an entity.</summary>
    Task RemoveAllTagsForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}