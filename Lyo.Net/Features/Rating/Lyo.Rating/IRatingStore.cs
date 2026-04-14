using Lyo.Common;

namespace Lyo.Rating;

/// <summary>Interface for storing and retrieving ratings.</summary>
public interface IRatingStore
{
    /// <summary>Adds or updates a rating.</summary>
    Task SaveAsync(RatingRecord rating, CancellationToken ct = default);

    /// <summary>Gets a rating by id.</summary>
    Task<RatingRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets all ratings for an entity (what is being rated).</summary>
    Task<IReadOnlyList<RatingRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default);

    /// <summary>Gets the rating for an entity from a specific rater and optional subject (e.g. one rating per user per entity per subject).</summary>
    Task<RatingRecord?> GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default);

    /// <summary>Adds or updates a reaction to a rating. One reaction per user per rating; switching from like to dislike updates the existing reaction.</summary>
    Task AddReactionAsync(EntityRef ratingRef, EntityRef fromEntity, RatingReactionType reactionType, CancellationToken ct = default);

    /// <summary>Removes a user's reaction from a rating.</summary>
    Task RemoveReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets a user's current reaction to a rating, or null if none.</summary>
    Task<RatingReactionRecord?> GetReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets all ratings created by an entity (e.g. all ratings from user 123).</summary>
    Task<IReadOnlyList<RatingRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets all ratings for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<RatingRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default);

    /// <summary>Deletes a rating by id.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Deletes the rating for an entity from a specific rater and optional subject.</summary>
    Task DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default);

    /// <summary>Deletes all ratings for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}