using System.ComponentModel.DataAnnotations;

namespace Lyo.Comment.Postgres.Database;

/// <summary>Entity for storing comment reactions (like/dislike) in PostgreSQL. Uses EntityRef structure.</summary>
public sealed class CommentReactionEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type of the comment (typically "Comment").</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the comment (typically the comment's Guid).</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the reactor (e.g. "User").</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the reactor.</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the reaction type (0=Like, 1=Dislike).</summary>
    public int ReactionType { get; set; }

    /// <summary>Gets or sets when the reaction was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }
}