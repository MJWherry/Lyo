using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Represents a comment attached to an entity (canonical entity-ref row + thread/reaction metadata).</summary>
public sealed class CommentRecord : EntityRefRow
{
    /// <summary>Comment body.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Parent comment id when this is a reply; null for top-level.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Cached like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Cached dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Last update time (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Whether the comment was edited after creation.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Gets the entity reference for what the comment is about.</summary>
    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who created the comment.</summary>
    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
