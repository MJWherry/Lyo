using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Rating.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lyo.Rating.Postgres;

/// <summary>PostgreSQL implementation of IRatingStore.</summary>
public sealed class PostgresRatingStore : EntityRefPostgresStoreBase, IRatingStore, IHealth
{
    private const string ModuleKey = "Rating";

    private readonly IDbContextFactory<RatingDbContext> _contextFactory;

    public PostgresRatingStore(
        IDbContextFactory<RatingDbContext> contextFactory,
        IOptions<EntityRefOptions> entityRefOptions,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
        : base(entityRefOptions, interceptors)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    private Guid Tenant => ResolveTenant(null);

    /// <inheritdoc />
    public async Task SaveAsync(RatingRecord rating, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(rating);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(rating.ForEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(rating.FromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Ratings.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(
                r => r.ForEntityType == rating.ForEntityType && r.ForEntityId == forId && r.FromEntityType == rating.FromEntityType && r.FromEntityId == fromId &&
                    r.Subject == rating.Subject, ct)
            .ConfigureAwait(false);

        if (existing != null) {
            existing.Value = rating.Value;
            existing.Title = rating.Title;
            existing.Message = rating.Message;
            existing.LikeCount = rating.LikeCount;
            existing.DislikeCount = rating.DislikeCount;
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforePersist, existing, ct).ConfigureAwait(false);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterPersist, existing, ct).ConfigureAwait(false);
        }
        else {
            var entity = new RatingEntity {
                Id = rating.Id == default ? Guid.NewGuid() : rating.Id,
                ForEntityType = rating.ForEntityType,
                ForEntityId = forId,
                FromEntityType = rating.FromEntityType,
                FromEntityId = fromId,
                TenantId = Tenant,
                Subject = rating.Subject,
                Title = rating.Title,
                Value = rating.Value,
                Message = rating.Message,
                LikeCount = rating.LikeCount,
                DislikeCount = rating.DislikeCount,
                Visibility = string.IsNullOrWhiteSpace(rating.Visibility) ? EntityRefVisibility.Private : rating.Visibility,
                CreatedAt = rating.CreatedAt == default ? DateTime.UtcNow : rating.CreatedAt
            };

            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforePersist, entity, ct).ConfigureAwait(false);
            context.Ratings.Add(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterPersist, entity, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<RatingRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Ratings.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RatingRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.WhereActive().WhereTenant(Tenant).Where(r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forId).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<RatingRecord?> GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Ratings.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(
                r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId &&
                    r.Subject == subject, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task AddReactionAsync(EntityRef ratingRef, EntityRef fromEntity, RatingReactionType reactionType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(ratingRef);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var ratingId = EntityRefPersistedGuid.RequirePersistedGuid(ratingRef);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rating = await context.Ratings.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(r => r.Id == ratingId, ct).ConfigureAwait(false);
        if (rating == null)
            return;

        var existing = await context.RatingReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == ratingRef.EntityType && r.ForEntityId == ratingId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId, ct)
            .ConfigureAwait(false);

        var reactionTypeInt = (int)reactionType;
        if (existing != null) {
            if (existing.ReactionType == reactionTypeInt)
                return;

            if (existing.ReactionType == (int)RatingReactionType.Like) {
                rating.LikeCount = Math.Max(0, rating.LikeCount - 1);
                rating.DislikeCount++;
            }
            else {
                rating.DislikeCount = Math.Max(0, rating.DislikeCount - 1);
                rating.LikeCount++;
            }

            existing.ReactionType = reactionTypeInt;
        }
        else {
            var reaction = new RatingReactionEntity {
                Id = Guid.NewGuid(),
                ForEntityType = ratingRef.EntityType,
                ForEntityId = ratingId,
                FromEntityType = fromEntity.EntityType,
                FromEntityId = fromId,
                ReactionType = reactionTypeInt,
                CreatedTimestamp = DateTime.UtcNow
            };

            context.RatingReactions.Add(reaction);
            if (reactionType == RatingReactionType.Like)
                rating.LikeCount++;
            else
                rating.DislikeCount++;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(ratingRef);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var ratingId = EntityRefPersistedGuid.RequirePersistedGuid(ratingRef);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var existing = await context.RatingReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == ratingRef.EntityType && r.ForEntityId == ratingId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId, ct)
            .ConfigureAwait(false);

        if (existing == null)
            return;
        var rating = await context.Ratings.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(r => r.Id == ratingId, ct).ConfigureAwait(false);
        if (rating != null) {
            if (existing.ReactionType == (int)RatingReactionType.Like)
                rating.LikeCount = Math.Max(0, rating.LikeCount - 1);
            else
                rating.DislikeCount = Math.Max(0, rating.DislikeCount - 1);
        }

        context.RatingReactions.Remove(existing);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RatingReactionRecord?> GetReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(ratingRef);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var ratingId = EntityRefPersistedGuid.RequirePersistedGuid(ratingRef);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var entity = await context.RatingReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == ratingRef.EntityType && r.ForEntityId == ratingId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToReactionRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RatingRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.WhereActive().WhereTenant(Tenant).Where(r => r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RatingRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Ratings.WhereActive().WhereTenant(Tenant).Where(r => r.ForEntityType == forEntityType);
        if (forEntityId.HasValue)
            query = query.Where(r => r.ForEntityId == forEntityId.Value);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Ratings.WhereActive().WhereTenant(Tenant).FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var reactions = await context.RatingReactions.Where(r => r.ForEntityType == "Rating" && r.ForEntityId == id).ToListAsync(ct).ConfigureAwait(false);
        context.RatingReactions.RemoveRange(reactions);
        await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, entity, ct).ConfigureAwait(false);
        entity.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var fromId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.WhereActive().WhereTenant(Tenant)
            .Where(r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forId && r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromId && r.Subject == subject)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ratingIds = entities.Select(e => e.Id).ToHashSet();
        var reactions = await context.RatingReactions.Where(r => r.ForEntityType == "Rating" && ratingIds.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.RatingReactions.RemoveRange(reactions);
        var utc = DateTime.UtcNow;
        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, e, ct).ConfigureAwait(false);

        foreach (var e in entities)
            e.DeletedAt = utc;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, e, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.WhereActive().WhereTenant(Tenant).Where(r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forId).ToListAsync(ct).ConfigureAwait(false);
        var ratingIds = entities.Select(e => e.Id).ToHashSet();
        var reactions = await context.RatingReactions.Where(r => r.ForEntityType == "Rating" && ratingIds.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.RatingReactions.RemoveRange(reactions);
        var utc = DateTime.UtcNow;
        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.BeforeSoftDelete, e, ct).ConfigureAwait(false);

        foreach (var e in entities)
            e.DeletedAt = utc;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, Tenant, EntityRefActionKind.AfterSoftDelete, e, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "rating-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "rating" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static RatingRecord ToRecord(RatingEntity e)
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
            Subject = e.Subject,
            Title = e.Title,
            Value = e.Value,
            Message = e.Message,
            LikeCount = e.LikeCount,
            DislikeCount = e.DislikeCount,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static RatingReactionRecord ToReactionRecord(RatingReactionEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            ReactionType = (RatingReactionType)e.ReactionType,
            CreatedTimestamp = e.CreatedTimestamp
        };
}
