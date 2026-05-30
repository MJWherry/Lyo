using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Represents a rating for an entity (canonical entity-ref row + rating fields).</summary>
public sealed class RatingRecord : EntityRelationRow
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
}
