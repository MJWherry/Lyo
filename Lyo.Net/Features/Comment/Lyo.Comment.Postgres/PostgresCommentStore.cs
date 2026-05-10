using System.Diagnostics;
using Lyo.Comment.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lyo.Comment.Postgres;

/// <summary>PostgreSQL implementation of ICommentStore.</summary>
public sealed class PostgresCommentStore : EntityRefPostgresStoreBase, ICommentStore, IHealth
{
    private const string ModuleKey = "Comment";

    private readonly IDbContextFactory<CommentDbContext> _contextFactory;

    public PostgresCommentStore(
        IDbContextFactory<CommentDbContext> contextFactory,
        IOptions<EntityRefOptions> entityRefOptions,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
        : base(entityRefOptions, interceptors)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    private Guid Tenant => ResolveTenant(null);

    /// <inheritdoc />
    public async Task SaveAsync(CommentRecord comment, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(comment);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(comment.ForEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(comment.FromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (comment.Id != default) {
            var existing = await context.Comments.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(c => c.Id == comment.Id, ct).ConfigureAwait(false);
            if (existing != null) {
                existing.ForEntityType = comment.ForEntityType;
                existing.ForEntityId = forId;
                existing.FromEntityType = comment.FromEntityType;
                existing.FromEntityId = fromId;
                existing.Content = comment.Content;
                existing.ReplyToCommentId = comment.ReplyToCommentId;
                existing.IsEdited = true;
                await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforePersist, existing, ct).ConfigureAwait(false);
                await context.SaveChangesAsync(ct).ConfigureAwait(false);
                await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterPersist, existing, ct).ConfigureAwait(false);
                return;
            }
        }

        var entity = new CommentEntity {
            Id = comment.Id == default ? Guid.NewGuid() : comment.Id,
            ForEntityType = comment.ForEntityType,
            ForEntityId = forId,
            FromEntityType = comment.FromEntityType,
            FromEntityId = fromId,
            TenantId = Tenant,
            Content = comment.Content,
            ReplyToCommentId = comment.ReplyToCommentId,
            LikeCount = comment.LikeCount,
            DislikeCount = comment.DislikeCount,
            IsEdited = comment.IsEdited,
            Visibility = string.IsNullOrWhiteSpace(comment.Visibility) ? EntityRefVisibility.Private : comment.Visibility,
            CreatedAt = comment.CreatedAt == default ? DateTime.UtcNow : comment.CreatedAt
        };

        await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforePersist, entity, ct).ConfigureAwait(false);
        context.Comments.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterPersist, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CommentRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Comments.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetForEntityAsync(EntityRef forEntity, bool includeReplies = true, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Comments.WhereActive().WhereTenant(Tenant).Where(c => c.ForEntityType == forEntity.EntityType && c.ForEntityId == forId);
        if (!includeReplies)
            query = query.Where(c => c.ReplyToCommentId == null);

        var entities = await query.OrderBy(c => c.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetRepliesAsync(Guid replyToCommentId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Comments.WhereActive().WhereTenant(Tenant).Where(c => c.ReplyToCommentId == replyToCommentId).OrderBy(c => c.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Comments.WhereActive().WhereTenant(Tenant).Where(c => c.FromEntityType == fromEntity.EntityType && c.FromEntityId == fromId).OrderBy(c => c.CreatedAt).ToListAsync(ct).ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CommentRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Comments.WhereActive().WhereTenant(Tenant).Where(c => c.ForEntityType == forEntityType);
        if (forEntityId.HasValue)
            query = query.Where(c => c.ForEntityId == forEntityId.Value);

        var entities = await query.OrderBy(c => c.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task AddReactionAsync(EntityRef commentRef, EntityRef fromEntity, CommentReactionType reactionType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(commentRef);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var commentId = EntityRefPersistedGuid.RequirePersistedGuid(commentRef);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var comment = await context.Comments.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(c => c.Id == commentId, ct).ConfigureAwait(false);
        if (comment == null)
            return;

        var existing = await context.CommentReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == commentRef.EntityType && r.ForEntityId == commentId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId, ct)
            .ConfigureAwait(false);

        var reactionTypeInt = (int)reactionType;
        if (existing != null) {
            if (existing.ReactionType == reactionTypeInt)
                return;

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
                ForEntityId = commentId,
                FromEntityType = fromEntity.EntityType,
                FromEntityId = fromId,
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
        ArgumentHelpers.ThrowIfNull(commentRef);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var commentId = EntityRefPersistedGuid.RequirePersistedGuid(commentRef);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var existing = await context.CommentReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == commentRef.EntityType && r.ForEntityId == commentId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId, ct)
            .ConfigureAwait(false);

        if (existing == null)
            return;
        var comment = await context.Comments.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(c => c.Id == commentId, ct).ConfigureAwait(false);
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
        ArgumentHelpers.ThrowIfNull(commentRef);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var commentId = EntityRefPersistedGuid.RequirePersistedGuid(commentRef);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var entity = await context.CommentReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == commentRef.EntityType && r.ForEntityId == commentId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToReactionRecord(entity);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, bool deleteReplies = false, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var ids = new List<Guid>();
        if (deleteReplies)
            ids.AddRange(await CollectDescendantIdsAsync(context, id, ct).ConfigureAwait(false));
        else
            ids.Add(id);

        var utc = DateTime.UtcNow;
        var toSoftDelete = new List<CommentEntity>();
        foreach (var cid in ids) {
            var c = await context.Comments.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(x => x.Id == cid, ct).ConfigureAwait(false);
            if (c != null)
                toSoftDelete.Add(c);
        }

        foreach (var c in toSoftDelete)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, c, ct).ConfigureAwait(false);

        foreach (var c in toSoftDelete)
            c.DeletedAt = utc;

        var idSet = ids.ToHashSet();
        var reactionsToDelete = await context.CommentReactions.Where(r => r.ForEntityType == "Comment" && idSet.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.CommentReactions.RemoveRange(reactionsToDelete);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var c in toSoftDelete)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, c, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var comments = await context.Comments.WhereActive().WhereTenant(Tenant).Where(c => c.ForEntityType == forEntity.EntityType && c.ForEntityId == forId).ToListAsync(ct).ConfigureAwait(false);
        var utc = DateTime.UtcNow;
        foreach (var c in comments)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, c, ct).ConfigureAwait(false);

        foreach (var c in comments)
            c.DeletedAt = utc;

        var commentIds = comments.Select(c => c.Id).ToHashSet();
        var reactions = await context.CommentReactions.Where(r => r.ForEntityType == "Comment" && commentIds.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.CommentReactions.RemoveRange(reactions);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var c in comments)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, c, ct).ConfigureAwait(false);
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

    private async Task<List<Guid>> CollectDescendantIdsAsync(CommentDbContext context, Guid rootId, CancellationToken ct)
    {
        var all = new List<Guid> { rootId };
        var frontier = new List<Guid> { rootId };
        while (frontier.Count > 0) {
            var next = await context.Comments.WhereActive().WhereTenant(Tenant).Where(c => c.ReplyToCommentId != null && frontier.Contains(c.ReplyToCommentId.Value)).Select(c => c.Id).ToListAsync(ct).ConfigureAwait(false);
            all.AddRange(next);
            frontier = next;
        }

        return all;
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
            TenantId = e.TenantId,
            Context = e.Context,
            CreatedAt = e.CreatedAt,
            ExpiresAt = e.ExpiresAt,
            DeletedAt = e.DeletedAt,
            DeletedByType = e.DeletedByType,
            DeletedById = e.DeletedById,
            MetadataJson = e.MetadataJson,
            Visibility = e.Visibility,
            Content = e.Content,
            ReplyToCommentId = e.ReplyToCommentId,
            LikeCount = e.LikeCount,
            DislikeCount = e.DislikeCount,
            UpdatedTimestamp = e.UpdatedTimestamp,
            IsEdited = e.IsEdited
        };
}
