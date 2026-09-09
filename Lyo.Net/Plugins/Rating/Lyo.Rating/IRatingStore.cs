using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Store contract for ratings.</summary>
/// <remarks>
/// Every read/write method takes an optional <c>Guid? tenantId</c>. A <c>null</c> falls back to <c>EntityRefOptions.DefaultTenantId</c> (single-tenant
/// hosts map that to <c>EntityRefWellKnown.SingleTenantDefaultId</c>).
/// </remarks>
public interface IRatingStore
{
    /// <summary>Upserts a rating.</summary>
    Task SaveAsync(RatingRecord rating, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Looks up a rating by id.</summary>
    Task<RatingRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists all ratings for an entity (what is being rated).</summary>
    Task<IReadOnlyList<RatingRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Rating for an entity from a specific rater and optional subject (e.g. one rating per user per entity per subject).</summary>
    Task<RatingRecord?> GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Upserts a reaction to a rating. One reaction per user per rating; switching from like to dislike updates the existing reaction.</summary>
    Task AddReactionAsync(EntityRef ratingRef, EntityRef fromEntity, RatingReactionType reactionType, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Drops a user's reaction from a rating.</summary>
    Task RemoveReactionAsync(EntityRef ratingRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns a user's current reaction to a rating, or null if none.</summary>
    Task<RatingReactionRecord?> GetReactionAsync(EntityRef ratingRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns every ratings created by an entity (e.g. all ratings from user 123).</summary>
    Task<IReadOnlyList<RatingRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns every ratings for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<RatingRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a rating by id.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes the rating for an entity from a specific rater and optional subject.</summary>
    Task DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes every ratings for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}