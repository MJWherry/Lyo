using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Represents a user's reaction (like/dislike) to a rating. ForEntity = the rating, FromEntity = who reacted.</summary>
public sealed class RatingReactionRecord
{
    public Guid Id { get; set; }

    public string ForEntityType { get; set; } = string.Empty;

    public Guid ForEntityId { get; set; }

    public string FromEntityType { get; set; } = string.Empty;

    public Guid FromEntityId { get; set; }

    public RatingReactionType ReactionType { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
