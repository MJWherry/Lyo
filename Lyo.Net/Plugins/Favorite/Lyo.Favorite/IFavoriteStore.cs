using Lyo.EntityReference.Models;

namespace Lyo.Favorite;

/// <summary>Store contract for favorites.</summary>
/// <remarks>
/// Stores take <see cref="EntityRef" /> at the API edge and persist <c>EntityId</c> as one Guid (Option A). Pass null for <c>tenantId</c> on methods in
/// single-tenant hosts (resolved through <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface IFavoriteStore
{
    /// <summary>Registers a favorite. If the same tenant/for/from triple already exists (active row), the call is a no-op.</summary>
    Task SaveAsync(FavoriteRecord favorite, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Returns a favorite by id (only non-deleted rows).</summary>
    Task<FavoriteRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Exposes the specific favorite record for a ForEntity/FromEntity pair, or null if not favorited.</summary>
    Task<FavoriteRecord?> GetAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>True if fromEntity has favorited forEntity.</summary>
    Task<bool> IsFavoritedAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Loads all favorites for an entity (everyone who has favorited it).</summary>
    Task<IReadOnlyList<FavoriteRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Lists all favorites added by an entity (everything it has favorited).</summary>
    Task<IReadOnlyList<FavoriteRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Lists all favorites for an entity type and optional for-entity id filter.</summary>
    Task<IReadOnlyList<FavoriteRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Exposes the number of favorites for an entity.</summary>
    Task<int> GetCountForEntityAsync(EntityRef forEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Returns favorite counts keyed by for-entity id. Missing ids are omitted (treat as zero).</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetFavoriteCountsForEntitiesAsync(
        string forEntityType,
        IReadOnlyList<Guid> forEntityIds,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Marks as deleted (soft) a favorite by id.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Marks as deleted (soft) the favorite for a specific ForEntity/FromEntity pair.</summary>
    Task DeleteAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Marks as deleted (soft) all favorites for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);

    /// <summary>Marks as deleted (soft) all favorites added by an entity.</summary>
    Task DeleteFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default);
}