using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Store contract for comments.</summary>
/// <remarks>
/// Stores take <see cref="EntityRef" /> at the API edge and persist <c>EntityId</c> as one Guid (Option A). Pass null for <c>tenantId</c> on methods in
/// single-tenant hosts (resolved through <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface ICommentStore
{
    /// <summary>Upserts a comment.</summary>
    Task SaveAsync(CommentRecord comment, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Looks up a comment by id.</summary>
    Task<CommentRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads all comments for an entity (top-level only, or all if includeReplies is true).</summary>
    Task<IReadOnlyList<CommentRecord>> GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns direct replies to a comment.</summary>
    Task<IReadOnlyList<CommentRecord>> GetRepliesAsync(Guid replyToCommentId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists all comments created by an entity.</summary>
    Task<IReadOnlyList<CommentRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads all comments for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<CommentRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Upserts a reaction to a comment. One reaction per user per comment; switching from like to dislike updates the existing reaction.</summary>
    Task AddReactionAsync(EntityRef commentRef, EntityRef fromEntity, CommentReactionType reactionType, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Drops a user's reaction from a comment.</summary>
    Task RemoveReactionAsync(EntityRef commentRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Returns a user's current reaction to a comment, or null if none.</summary>
    Task<CommentReactionRecord?> GetReactionAsync(EntityRef commentRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a comment by id (and optionally its replies).</summary>
    Task DeleteAsync(Guid id, bool deleteReplies = false, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes every comments for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}