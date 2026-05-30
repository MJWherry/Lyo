using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Represents a user's reaction (like/dislike) to a comment. ForEntity = the comment, FromEntity = who reacted.</summary>
public sealed class CommentReactionRecord
{
    public Guid Id { get; set; }

    public string? SubjectEntityType { get; set; }

    public string? SubjectEntityId { get; set; }

    public string? ActorEntityType { get; set; }

    public string? ActorEntityId { get; set; }

    public Guid? TenantId { get; set; }

    public CommentReactionType ReactionType { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public EntityRef ForEntity => EntityRef.ForKey(SubjectEntityType ?? string.Empty, SubjectEntityId ?? string.Empty);

    public EntityRef FromEntity => EntityRef.ForKey(ActorEntityType ?? string.Empty, ActorEntityId ?? string.Empty);
}
