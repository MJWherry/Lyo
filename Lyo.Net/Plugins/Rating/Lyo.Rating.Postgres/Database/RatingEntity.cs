using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Postgres row that holds ratings in PostgreSQL.</summary>
public sealed class RatingEntity : EntityRelationEntityBase
{
    /// <summary>Subject axis.</summary>
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