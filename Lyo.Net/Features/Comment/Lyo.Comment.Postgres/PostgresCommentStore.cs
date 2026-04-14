using System.Diagnostics;
using Lyo.Comment.Postgres.Database;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Comment.Postgres;

/// <summary>PostgreSQL implementation of ICommentStore.</summary>
public sealed class PostgresCommentStore : ICommentStore, IHealth
{
    private readonly IDbContextFactory<CommentDbContext> _contextFactory;

    /// <summary>Creates a new PostgresCommentStore.</summary>
    public PostgresCommentStore(IDbContextFactory<CommentDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task SaveAsync(CommentRecord comment, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(comment, nameof(comment));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (comment.Id != default) {
            var existing = await context.Comments.FindAsync([comment.Id], ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ForEntityType = comment.ForEntityType;
                existing.ForEntityId = comment.ForEntityId;
                existing.FromEntityType = comment.FromEntityType;
                existing.FromEntityId = comment.FromEntityId;
                existing.Content = comment.Content;
                existing.ReplyToCommentId = comment.ReplyToCommentId;
                existing.IsEdited = true;
                existing.UpdatedTimestamp = DateTime.UtcNow;
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new CommentEntity {
            Id = comment.Id == default ? Guid.NewGuid() : comment.Id,
            ForEntityType = comment.ForEntityType,
            ForEntityId = comment.ForEntityId,
            FromEntityType = comment.FromEntityType,
            FromEntityId = comment.FromEntityId,
            Content = comment.Content,
            ReplyToCommentId = comment.ReplyToCommentId,
            LikeCount = comment.LikeCount,
            DislikeCount = comment.DislikeCount,
            IsEdited = comment.IsEdited,
            CreatedTimestamp = comment.CreatedTimestamp == default ? DateTime.UtcNow : comment.CreatedTimestamp
        };

        context.Comments.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CommentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Comments.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Comments.Where(c => c.ForEntityType == forEntity.EntityType && c.ForEntityId == forEntity.EntityId);
        if (!includeReplies)
            query = query.Where(c => c.ReplyToCommentId == null);

        var entities = await query.OrderBy(c => c.CreatedTimestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetRepliesAsync(Guid replyToCommentId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Comments.Where(c => c.ReplyToCommentId == replyToCommentId).OrderBy(c => c.CreatedTimestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Comments.Where(c => c.FromEntityType == fromEntity.EntityType && c.FromEntityId == fromEntity.EntityId)
            .OrderBy(c => c.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Comments.Where(c => c.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(forEntityId))
            query = query.Where(c => c.ForEntityId == forEntityId);

        var entities = await query.OrderBy(c => c.CreatedTimestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task AddReactionAsync(EntityRef commentRef, EntityRef fromEntity, CommentReactionType reactionType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(commentRef, nameof(commentRef));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        if (!Guid.TryParse(commentRef.EntityId, out var commentId))
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var comment = await context.Comments.FindAsync([commentId], ct).ConfigureAwait(false);
        if (comment == null)
            return;

        var existing = await context.CommentReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == commentRef.EntityType && r.ForEntityId == commentRef.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        var reactionTypeInt = (int)reactionType;
        if (existing != null) {
            if (existing.ReactionType == reactionTypeInt)
                return; // Already has this reaction

            // Switching reaction: decrement old, increment new
            if (existing.ReactionType == (int)CommentReactionType.Like) {
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                comment.DislikeCount++;
            }
            else {
                comment.DislikeCount = Math.Max(0, comment.DislikeCount - 1);
                comment.LikeCount++;
            }

            existing.ReactionType = reactionTypeInt;
        }
        else {
            var reaction = new CommentReactionEntity {
                Id = Guid.NewGuid(),
                ForEntityType = commentRef.EntityType,
                ForEntityId = commentRef.EntityId,
                FromEntityType = fromEntity.EntityType,
                FromEntityId = fromEntity.EntityId,
                ReactionType = reactionTypeInt,
                CreatedTimestamp = DateTime.UtcNow
            };

            context.CommentReactions.Add(reaction);
            if (reactionType == CommentReactionType.Like)
                comment.LikeCount++;
            else
                comment.DislikeCount++;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveReactionAsync(EntityRef commentRef, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(commentRef, nameof(commentRef));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        if (!Guid.TryParse(commentRef.EntityId, out var commentId))
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.CommentReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == commentRef.EntityType && r.ForEntityId == commentRef.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        if (existing == null)
            return;

        var comment = await context.Comments.FindAsync([commentId], ct).ConfigureAwait(false);
        if (comment != null) {
            if (existing.ReactionType == (int)CommentReactionType.Like)
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
            else
                comment.DislikeCount = Math.Max(0, comment.DislikeCount - 1);
        }

        context.CommentReactions.Remove(existing);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CommentReactionRecord?> GetReactionAsync(EntityRef commentRef, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(commentRef, nameof(commentRef));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.CommentReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == commentRef.EntityType && r.ForEntityId == commentRef.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToReactionRecord(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, bool deleteReplies = false, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var commentIds = new List<Guid> { id };
        if (deleteReplies) {
            var currentLevel = new List<Guid> { id };
            while (currentLevel.Count > 0) {
                var replyIds = await context.Comments.Where(c => c.ReplyToCommentId != null && currentLevel.Contains(c.ReplyToCommentId.Value))
                    .Select(c => c.Id)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                commentIds.AddRange(replyIds);
                currentLevel = replyIds;
            }
        }

        var commentIdStrings = commentIds.Select(i => i.ToString()).ToHashSet();
        var reactionsToDelete = await context.CommentReactions.Where(r => r.ForEntityType == "Comment" && commentIdStrings.Contains(r.ForEntityId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        context.CommentReactions.RemoveRange(reactionsToDelete);
        if (deleteReplies) {
            var comments = await context.Comments.Where(c => commentIds.Contains(c.Id)).ToListAsync(ct).ConfigureAwait(false);
            context.Comments.RemoveRange(comments);
        }
        else {
            var entity = await context.Comments.FindAsync([id], ct).ConfigureAwait(false);
            if (entity != null)
                context.Comments.Remove(entity);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var comments = await context.Comments.Where(c => c.ForEntityType == forEntity.EntityType && c.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        var commentIds = comments.Select(c => c.Id.ToString()).ToHashSet();
        var reactions = await context.CommentReactions.Where(r => r.ForEntityType == "Comment" && commentIds.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.CommentReactions.RemoveRange(reactions);
        context.Comments.RemoveRange(comments);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "comment-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "comment" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static CommentReactionRecord ToReactionRecord(CommentReactionEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            ReactionType = (CommentReactionType)e.ReactionType,
            CreatedTimestamp = e.CreatedTimestamp
        };

    private static CommentRecord ToRecord(CommentEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            Content = e.Content,
            ReplyToCommentId = e.ReplyToCommentId,
            LikeCount = e.LikeCount,
            DislikeCount = e.DislikeCount,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp,
            IsEdited = e.IsEdited
        };
}