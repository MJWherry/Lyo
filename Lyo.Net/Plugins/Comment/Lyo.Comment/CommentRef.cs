using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Builds an EntityRef for comments (used when adding reactions).</summary>
public static class CommentRef
{
    /// <summary>Builds an EntityRef for a comment by id. Use when calling AddReactionAsync, RemoveReactionAsync, GetReactionAsync.</summary>
    public static EntityRef ForComment(Guid commentId) => EntityRef.ForKey("Comment", commentId.ToString());
}