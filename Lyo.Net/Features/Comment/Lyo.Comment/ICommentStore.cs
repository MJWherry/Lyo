using Lyo.Common;

namespace Lyo.Comment;

/// <summary>Interface for storing and retrieving comments.</summary>
public interface ICommentStore
{
    /// <summary>Adds or updates a comment.</summary>
    Task SaveAsync(CommentRecord comment, CancellationToken ct = default);

    /// <summary>Gets a comment by id.</summary>
    Task<CommentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Gets all comments for an entity (top-level only, or all if includeReplies is true).</summary>
    Task<IReadOnlyList<CommentRecord>> GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, CancellationToken ct = default);

    /// <summary>Gets direct replies to a comment.</summary>
    Task<IReadOnlyList<CommentRecord>> GetRepliesAsync(Guid replyToCommentId, CancellationToken ct = default);

    /// <summary>Gets all comments created by an entity.</summary>
    Task<IReadOnlyList<CommentRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets all comments for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<CommentRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default);

    /// <summary>Adds or updates a reaction to a comment. One reaction per user per comment; switching from like to dislike updates the existing reaction.</summary>
    /// <param name="commentRef">EntityRef for the comment (e.g. EntityRef.ForKey("Comment", commentId.ToString()))</param>
    /// <param name="fromEntity">EntityRef for who is reacting (e.g. the user)</param>
    /// <param name="reactionType">Like or Dislike</param>
    Task AddReactionAsync(EntityRef commentRef, EntityRef fromEntity, CommentReactionType reactionType, CancellationToken ct = default);

    /// <summary>Removes a user's reaction from a comment.</summary>
    Task RemoveReactionAsync(EntityRef commentRef, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Gets a user's current reaction to a comment, or null if none.</summary>
    Task<CommentReactionRecord?> GetReactionAsync(EntityRef commentRef, EntityRef fromEntity, CancellationToken ct = default);

    /// <summary>Deletes a comment by id (and optionally its replies).</summary>
    Task DeleteAsync(Guid id, bool deleteReplies = false, CancellationToken ct = default);

    /// <summary>Deletes all comments for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default);
}