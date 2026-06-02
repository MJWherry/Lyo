using System.Diagnostics;

namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request to add a tag to a comic entity.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AddTagReq
{
    /// <summary>Display / query tag value (e.g. "Fantasy").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tag classification (e.g. "tag", "category"). Defaults to "tag".</summary>
    public string TagType { get; set; } = "tag";

    /// <summary>Optional URL-friendly slug for this tag assignment; stored empty when omitted.</summary>
    public string? Slug { get; set; }

    public override string ToString() => $"AddTagReq: name={Name}, type={TagType}";
}

/// <summary>Request to add or update a rating on a comic entity.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AddRatingReq
{
    public string ActorEntityType { get; set; } = string.Empty;

    public string ActorEntityId { get; set; } = string.Empty;

    /// <summary>Optional subject for the rating (e.g. "art", "story"). Null = general rating.</summary>
    public string? Subject { get; set; }

    /// <summary>Optional title for the review.</summary>
    public string? Title { get; set; }

    /// <summary>Optional numeric rating value (e.g. 1–10 stars).</summary>
    public decimal? Value { get; set; }

    /// <summary>Optional written review message.</summary>
    public string? Message { get; set; }

    public override string ToString() => $"AddRatingReq: actor={ActorEntityType}:{ActorEntityId}, value={Value}, subject={Subject}";
}

/// <summary>Request to add a comment to a comic entity.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AddCommentReq
{
    public string ActorEntityType { get; set; } = string.Empty;

    public string ActorEntityId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>The comment this is a reply to, or null if top-level.</summary>
    public Guid? ReplyToCommentId { get; set; }

    public override string ToString() => $"AddCommentReq: actor={ActorEntityType}:{ActorEntityId}, replyTo={ReplyToCommentId}";
}

/// <summary>Request to favorite a comic entity.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AddFavoriteReq
{
    public string ActorEntityType { get; set; } = string.Empty;

    public string ActorEntityId { get; set; } = string.Empty;

    public override string ToString() => $"AddFavoriteReq: actor={ActorEntityType}:{ActorEntityId}";
}

/// <summary>Request to remove a favorite from a comic entity.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RemoveFavoriteReq
{
    public string ActorEntityType { get; set; } = string.Empty;

    public string ActorEntityId { get; set; } = string.Empty;

    public override string ToString() => $"RemoveFavoriteReq: actor={ActorEntityType}:{ActorEntityId}";
}