using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Builds an EntityRef for ratings (used when adding reactions).</summary>
public static class RatingRef
{
    /// <summary>Produces an EntityRef for a rating by id. Use when calling AddReactionAsync, RemoveReactionAsync, GetReactionAsync.</summary>
    public static EntityRef ForRating(Guid ratingId) => EntityRef.ForKey("Rating", ratingId.ToString());
}