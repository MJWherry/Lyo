using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Represents a user's reaction (like/dislike) to a comment. ForEntity = the comment, FromEntity = who reacted.</summary>
public sealed class CommentReactionRecord
{
    public Guid Id { get; set; }

    public string ForEntityType { get; set; } = string.Empty;

    public Guid ForEntityId { get; set; }

    public string FromEntityType { get; set; } = string.Empty;

    public Guid FromEntityId { get; set; }

    public CommentReactionType ReactionType { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public EntityRef ForEntity => EntityRef.ForGuid(ForEntityType, ForEntityId);

    public EntityRef FromEntity => EntityRef.ForGuid(FromEntityType, FromEntityId);
}
