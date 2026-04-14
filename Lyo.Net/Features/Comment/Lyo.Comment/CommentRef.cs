using Lyo.Common;

namespace Lyo.Comment;

/// <summary>Helper for creating EntityRef for comments (used when adding reactions).</summary>
public static class CommentRef
{
    /// <summary>Creates an EntityRef for a comment by id. Use when calling AddReactionAsync, RemoveReactionAsync, GetReactionAsync.</summary>
    public static EntityRef ForComment(Guid commentId) => EntityRef.ForKey("Comment", commentId.ToString());
}