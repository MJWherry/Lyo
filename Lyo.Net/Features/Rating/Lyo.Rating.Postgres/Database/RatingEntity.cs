using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Entity for storing ratings in PostgreSQL.</summary>
public sealed class RatingEntity : EntityRefEntityBase
{
    /// <summary>Optional subject axis.</summary>
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
