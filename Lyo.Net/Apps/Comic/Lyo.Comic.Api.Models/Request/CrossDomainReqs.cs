namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request to add a tag to a comic entity.</summary>
public sealed class AddTagReq
{
    public string Tag { get; set; } = string.Empty;
}

/// <summary>Request to add or update a rating on a comic entity.</summary>
public sealed class AddRatingReq
{
    public string FromEntityType { get; set; } = string.Empty;
    public string FromEntityId { get; set; } = string.Empty;

    /// <summary>Optional subject for the rating (e.g. "art", "story"). Null = general rating.</summary>
    public string? Subject { get; set; }

    /// <summary>Optional title for the review.</summary>
    public string? Title { get; set; }

    /// <summary>Optional numeric rating value (e.g. 1–10 stars).</summary>
    public decimal? Value { get; set; }

    /// <summary>Optional written review message.</summary>
    public string? Message { get; set; }
}

/// <summary>Request to add a comment to a comic entity.</summary>
public sealed class AddCommentReq
{
    public string FromEntityType { get; set; } = string.Empty;
    public string FromEntityId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    /// <summary>The comment this is a reply to, or null if top-level.</summary>
    public Guid? ReplyToCommentId { get; set; }
}

/// <summary>Request to favorite a comic entity.</summary>
public sealed class AddFavoriteReq
{
    public string FromEntityType { get; set; } = string.Empty;
    public string FromEntityId { get; set; } = string.Empty;
}

/// <summary>Request to remove a favorite from a comic entity.</summary>
public sealed class RemoveFavoriteReq
{
    public string FromEntityType { get; set; } = string.Empty;
    public string FromEntityId { get; set; } = string.Empty;
}
