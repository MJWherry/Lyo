using Lyo.EntityReference.Models;

namespace Lyo.Comment;

/// <summary>Interface for storing and retrieving comments.</summary>
/// <remarks>
/// Stores accept <see cref="EntityRef" /> at the API boundary but persist <c>EntityId</c> as a single Guid per Option A. Pass null for <c>tenantId</c> on methods in
/// single-tenant deployments (resolved via <see cref="EntityRefOptions.DefaultTenantId" />).
/// </remarks>
public interface ICommentStore
{
    /// <summary>Adds or updates a comment.</summary>
    Task SaveAsync(CommentRecord comment, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets a comment by id.</summary>
    Task<CommentRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all comments for an entity (top-level only, or all if includeReplies is true).</summary>
    Task<IReadOnlyList<CommentRecord>> GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets direct replies to a comment.</summary>
    Task<IReadOnlyList<CommentRecord>> GetRepliesAsync(Guid replyToCommentId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all comments created by an entity.</summary>
    Task<IReadOnlyList<CommentRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets all comments for an entity type and optional entity id filter.</summary>
    Task<IReadOnlyList<CommentRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Adds or updates a reaction to a comment. One reaction per user per comment; switching from like to dislike updates the existing reaction.</summary>
    Task AddReactionAsync(EntityRef commentRef, EntityRef fromEntity, CommentReactionType reactionType, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a user's reaction from a comment.</summary>
    Task RemoveReactionAsync(EntityRef commentRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Gets a user's current reaction to a comment, or null if none.</summary>
    Task<CommentReactionRecord?> GetReactionAsync(EntityRef commentRef, EntityRef fromEntity, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes a comment by id (and optionally its replies).</summary>
    Task DeleteAsync(Guid id, bool deleteReplies = false, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes all comments for an entity.</summary>
    Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, CancellationToken ct = default);
}