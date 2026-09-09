using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Model for a comment attached to an entity (canonical entity-ref row + thread/reaction metadata).</summary>
public sealed class CommentRecord : EntityRelationRow
{
    /// <summary>Comment text.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Top-level parent comment id when this is a reply; null.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Memoized like total.</summary>
    public int LikeCount { get; set; }

    /// <summary>Memoized dislike total.</summary>
    public int DislikeCount { get; set; }

    /// <summary>UTC time of the last write.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>True when the comment was edited after creation.</summary>
    public bool IsEdited { get; set; }
}