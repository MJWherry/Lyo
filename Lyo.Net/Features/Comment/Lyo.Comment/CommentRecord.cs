using Lyo.Common;

namespace Lyo.Comment;

/// <summary>Represents a comment attached to an entity, optionally in reply to another comment.</summary>
public sealed class CommentRecord
{
    /// <summary>Gets or sets the unique identifier of the comment.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type the comment is for (e.g. "Docket", "Person").</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id the comment is for.</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the author (e.g. "User").</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the author.</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the comment content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the comment this is a reply to, or null if top-level.</summary>
    public Guid? ReplyToCommentId { get; set; }

    /// <summary>Gets or sets the like count.</summary>
    public int LikeCount { get; set; }

    /// <summary>Gets or sets the dislike count.</summary>
    public int DislikeCount { get; set; }

    /// <summary>Gets or sets when the comment was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when the comment was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets or sets whether the comment has been edited.</summary>
    public bool IsEdited { get; set; }

    /// <summary>Gets the entity reference for what the comment is about.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who created the comment.</summary>
    public EntityRef FromEntity => new(FromEntityType, FromEntityId);
}