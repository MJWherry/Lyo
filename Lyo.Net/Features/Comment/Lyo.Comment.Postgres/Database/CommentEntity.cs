using System.ComponentModel.DataAnnotations;

namespace Lyo.Comment.Postgres.Database;

/// <summary>Entity for storing comments in PostgreSQL.</summary>
public sealed class CommentEntity
{
    /// <summary>Gets or sets the unique identifier (UUID).</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type the comment is for.</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id the comment is for.</summary>
    [Required]
    [MaxLength(200)]
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the author.</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the author.</summary>
    [Required]
    [MaxLength(200)]
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the comment content.</summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the comment this is a reply to, or null if top-level.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Gets or sets the like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Gets or sets the dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Gets or sets when the comment was created.</summary>
    [Required]
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the comment was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets or sets whether the comment has been edited.</summary>
    public bool IsEdited { get; set; }
}