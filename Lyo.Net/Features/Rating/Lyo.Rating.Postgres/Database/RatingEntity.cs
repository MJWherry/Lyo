using System.ComponentModel.DataAnnotations;

namespace Lyo.Rating.Postgres.Database;

/// <summary>Entity for storing ratings in PostgreSQL.</summary>
public sealed class RatingEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type being rated (e.g. "Person", "Docket").</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id being rated.</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the rater (e.g. "User").</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the rater.</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional subject (e.g. "scary", "action"). Null = general rating.</summary>
    [MaxLength(200)]
    public string? Subject { get; set; }

    /// <summary>Gets or sets the optional title for the rating/review.</summary>
    [MaxLength(500)]
    public string? Title { get; set; }

    /// <summary>Gets or sets the optional rating value. Null = review without numeric score.</summary>
    public decimal? Value { get; set; }

    /// <summary>Gets or sets the optional review message.</summary>
    [MaxLength(4000)]
    public string? Message { get; set; }

    /// <summary>Gets or sets the like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Gets or sets the dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Gets or sets when the rating was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the rating was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}