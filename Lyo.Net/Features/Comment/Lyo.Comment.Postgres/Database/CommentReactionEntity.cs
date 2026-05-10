using System.ComponentModel.DataAnnotations;

namespace Lyo.Comment.Postgres.Database;

/// <summary>Entity for storing comment reactions (like/dislike) in PostgreSQL. Option A uuid keys for target comment and reactor.</summary>
public sealed class CommentReactionEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ForEntityType { get; set; } = string.Empty;

    public Guid ForEntityId { get; set; }

    [Required]
    [MaxLength(200)]
    public string FromEntityType { get; set; } = string.Empty;

    public Guid FromEntityId { get; set; }

    public int ReactionType { get; set; }

    [Required]
    public DateTime CreatedTimestamp { get; set; }
}
