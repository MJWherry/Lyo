using Lyo.EntityReference.Postgres.Database;

namespace Lyo.Comment.Postgres.Database;

/// <summary>Postgres row that holds comments in PostgreSQL.</summary>
public sealed class CommentEntity : EntityRelationEntityBase
{
    /// <summary>Comment text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Parent comment id for a reply.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Memoized like total.</summary>
    public int LikeCount { get; set; }

    /// <summary>Memoized dislike total.</summary>
    public int DislikeCount { get; set; }

    /// <summary>UTC time of the last write.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Flag: the comment was edited.</summary>
    public bool IsEdited { get; set; }
}