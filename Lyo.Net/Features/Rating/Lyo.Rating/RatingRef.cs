using Lyo.Common;

namespace Lyo.Rating;

/// <summary>Helper for creating EntityRef for ratings (used when adding reactions).</summary>
public static class RatingRef
{
    /// <summary>Creates an EntityRef for a rating by id. Use when calling AddReactionAsync, RemoveReactionAsync, GetReactionAsync.</summary>
    public static EntityRef ForRating(Guid ratingId) => EntityRef.ForKey("Rating", ratingId.ToString());
}