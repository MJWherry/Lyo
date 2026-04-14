using Lyo.Common;

namespace Lyo.Comment;

/// <summary>Represents a user's reaction (like/dislike) to a comment. Uses EntityRef: ForEntity = the comment, FromEntity = who reacted.</summary>
public sealed class CommentReactionRecord
{
    /// <summary>Gets or sets the unique identifier of the reaction.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the entity type of the comment being reacted to (typically "Comment").</summary>
    public string ForEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the comment (typically the comment's Guid).</summary>
    public string ForEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type of the reactor (e.g. "User").</summary>
    public string FromEntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity id of the reactor.</summary>
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the reaction type.</summary>
    public CommentReactionType ReactionType { get; set; }

    /// <summary>Gets or sets when the reaction was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets the entity reference for the comment being reacted to.</summary>
    public EntityRef ForEntity => new(ForEntityType, ForEntityId);

    /// <summary>Gets the entity reference for who reacted.</summary>
    public EntityRef FromEntity => new(FromEntityType, FromEntityId);
}