using Lyo.Common.Identifiers;

namespace Lyo.Favorite;

/// <summary>Interface for storing and retrieving favorites.</summary>
public interface IFavoriteStore
{
    /// <summary>Adds a favorite. If the same ForEntity/FromEntity pair already exists, the call is a no-op.</summary>
    Task SaveAsync(FavoriteRecord favorite, CancellationToken ct = default);

    /// <summary>Gets a favorite by id.</summary>
    Task<FavoriteRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets the specific favorite record for a ForEntity/FromEntity pair, or null if not favorited.</summary>
    Task<FavoriteRecord?> GetAsync(EntityRef forEntity, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Returns true if fromEntity has favorited forEntity.</summary>
    Task<bool> IsFavoritedAsync(EntityRef forEntity, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets all favorites for an entity (everyone who has favorited it).</summary>
    Task<IReadOnlyList<FavoriteRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Gets all favorites added by an entity (everything it has favorited).</summary>
    Task<IReadOnlyList<FavoriteRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets all favorites for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<FavoriteRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default);

    /// <summary>Gets the number of favorites for an entity.</summary>
    Task<int> GetCountForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Gets favorite counts keyed by for-entity id. Missing ids are omitted (treat as zero).</summary>
    Task<IReadOnlyDictionary<string, int>> GetFavoriteCountsForEntitiesAsync(string forEntityType, IReadOnlyList<string> forEntityIds, CancellationToken ct = default);

    /// <summary>Deletes a favorite by id.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deletes the favorite for a specific ForEntity/FromEntity pair.</summary>
    Task DeleteAsync(EntityRef forEntity, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Deletes all favorites for an entity (removes all who have favorited it).</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Deletes all favorites added by an entity (removes everything it has favorited).</summary>
    Task DeleteFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default);
}