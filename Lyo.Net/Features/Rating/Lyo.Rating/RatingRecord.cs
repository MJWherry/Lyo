using Lyo.Common;

namespace Lyo.Rating;

/// <summary>Represents a rating for an entity.</summary>
public sealed class RatingRecord
{
    /// <summary>Gets or sets the unique identifier of the rating.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being rated (e.g. "Person", "Docket").</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id being rated (typically a Guid string, or any string key).</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the rater (e.g. "User").</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the rater (e.g. user id 123).</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional subject (e.g. "scary", "action"). Null = general rating. Allows multiple ratings per entity per user.</summary>
    public string? Subject { get; set; }

    /// <summary>Gets or sets the optional title for the rating/review.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the optional rating value (e.g. 1–5 stars). Null = review without numeric score.</summary>
    public decimal? Value { get; set; }

    /// <summary>Gets or sets the optional review message (written review accompanying the rating).</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Gets or sets the dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Gets or sets when the rating was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the rating was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for what is being rated.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who created the rating.</summary>
    public EntityRef FromEntity => new(FromEntityType, FromEntityId);
}