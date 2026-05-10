using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Represents a rating for an entity (canonical entity-ref row + rating fields).</summary>
public sealed class RatingRecord : EntityRefRow
{
    /// <summary>Optional subject (e.g. genre axis); null = general rating.</summary>
    public string? Subject { get; set; }

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional numeric score.</summary>
    public decimal? Value { get; set; }

    /// <summary>Optional review text.</summary>
    public string? Message { get; set; }

    /// <summary>Cached like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Cached dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for what is being rated.</summary>
    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who created the rating.</summary>
    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
