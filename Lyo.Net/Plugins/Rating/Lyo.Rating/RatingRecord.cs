using Lyo.EntityReference.Models;

namespace Lyo.Rating;

/// <summary>Model for a rating for an entity (canonical entity-ref row + rating fields).</summary>
public sealed class RatingRecord : EntityRelationRow
{
    /// <summary>subject (e.g. genre axis); null = general rating. May be omitted.</summary>
    public string? Subject { get; set; }

    /// <summary>Title when one is supplied.</summary>
    public string? Title { get; set; }

    /// <summary>Numeric score when present.</summary>
    public decimal? Value { get; set; }

    /// <summary>Review text when present.</summary>
    public string? Message { get; set; }

    /// <summary>Memoized like total.</summary>
    public int LikeCount { get; set; }

    /// <summary>Memoized dislike total.</summary>
    public int DislikeCount { get; set; }

    /// <summary>UTC time of the last write.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}