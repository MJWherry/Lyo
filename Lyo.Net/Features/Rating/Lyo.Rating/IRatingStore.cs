using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Interface for storing and retrieving ratings.</summary>
/// <remarks>
/// All read/write methods accept an optional <c>Guid? tenantId</c>. When <c>null</c>, the store falls back to <c>EntityRefOptions.DefaultTenantId</c> (single-tenant
/// deployments resolve this to <c>EntityRefWellKnown.SingleTenantDefaultId</c>).
/// </remarks>
public interface IRatingStore
{
    /// <summary>Adds or updates a rating.</summary>
    Task SaveAsync(RatingRecord rating, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets a rating by id.</summary>
    Task<RatingRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all ratings for an entity (what is being rated).</summary>
    Task<IReadOnlyList<RatingRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets the rating for an entity from a specific rater and optional subject (e.g. one rating per user per entity per subject).</summary>
    Task<RatingRecord?> GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Adds or updates a reaction to a rating. One reaction per user per rating; switching from like to dislike updates the existing reaction.</summary>
    Task AddReactionAsync(EntityRef ratingRef, EntityRef fromEntity, RatingReactionType reactionType, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a user's reaction from a rating.</summary>
    Task RemoveReactionAsync(EntityRef ratingRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets a user's current reaction to a rating, or null if none.</summary>
    Task<RatingReactionRecord?> GetReactionAsync(EntityRef ratingRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all ratings created by an entity (e.g. all ratings from user 123).</summary>
    Task<IReadOnlyList<RatingRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all ratings for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<RatingRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes a rating by id.</summary>
    Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes the rating for an entity from a specific rater and optional subject.</summary>
    Task DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes all ratings for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}