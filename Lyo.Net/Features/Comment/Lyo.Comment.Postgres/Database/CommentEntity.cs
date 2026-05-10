using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Comment.Postgres.Database;

/// <summary>Entity for storing comments in PostgreSQL.</summary>
public sealed class CommentEntity : EntityRefEntityBase
{
    /// <summary>Comment body.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Parent comment id when this is a reply.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Cached like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Cached dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Whether the comment was edited.</summary>
    public bool IsEdited { get; set; }
}
