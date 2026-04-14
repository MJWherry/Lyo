using System.Diagnostics;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Rating.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Rating.Postgres;

/// <summary>PostgreSQL implementation of IRatingStore.</summary>
public sealed class PostgresRatingStore : IRatingStore, IHealth
{
    private readonly IDbContextFactory<RatingDbContext> _contextFactory;

    /// <summary>Creates a new PostgresRatingStore.</summary>
    public PostgresRatingStore(IDbContextFactory<RatingDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
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

    /// <inheritdoc />
    public async Task SaveAsync(RatingRecord rating, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(rating, nameof(rating));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Ratings.FirstOrDefaultAsync(
                r => r.ForEntityType == rating.ForEntityType && r.ForEntityId == rating.ForEntityId && r.FromEntityType == rating.FromEntityType &&
                    r.FromEntityId == rating.FromEntityId && r.Subject == rating.Subject, ct)
            .ConfigureAwait(false);

        if (existing != null) {
            existing.Value = rating.Value;
            existing.Title = rating.Title;
            existing.Message = rating.Message;
            existing.UpdatedTimestamp = DateTime.UtcNow;
        }
        else {
            var entity = new RatingEntity {
                Id = rating.Id == default ? Guid.NewGuid() : rating.Id,
                ForEntityType = rating.ForEntityType,
                ForEntityId = rating.ForEntityId,
                FromEntityType = rating.FromEntityType,
                FromEntityId = rating.FromEntityId,
                Subject = rating.Subject,
                Title = rating.Title,
                Value = rating.Value,
                Message = rating.Message,
                LikeCount = rating.LikeCount,
                DislikeCount = rating.DislikeCount,
                CreatedTimestamp = rating.CreatedTimestamp == default ? DateTime.UtcNow : rating.CreatedTimestamp
            };

            context.Ratings.Add(entity);
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RatingRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Ratings.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RatingRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.Where(r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<RatingRecord?> GetForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Ratings.FirstOrDefaultAsync(
                r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forEntity.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId && r.Subject == subject, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task AddReactionAsync(EntityRef ratingRef, EntityRef fromEntity, RatingReactionType reactionType, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(ratingRef, nameof(ratingRef));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        if (!Guid.TryParse(ratingRef.EntityId, out var ratingId))
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rating = await context.Ratings.FindAsync([ratingId], ct).ConfigureAwait(false);
        if (rating == null)
            return;

        var existing = await context.RatingReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == ratingRef.EntityType && r.ForEntityId == ratingRef.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId, ct)
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
                ForEntityId = ratingRef.EntityId,
                FromEntityType = fromEntity.EntityType,
                FromEntityId = fromEntity.EntityId,
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
        ArgumentHelpers.ThrowIfNull(ratingRef, nameof(ratingRef));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.RatingReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == ratingRef.EntityType && r.ForEntityId == ratingRef.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        if (existing == null)
            return;

        if (Guid.TryParse(ratingRef.EntityId, out var ratingId)) {
            var rating = await context.Ratings.FindAsync([ratingId], ct).ConfigureAwait(false);
            if (rating != null) {
                if (existing.ReactionType == (int)RatingReactionType.Like)
                    rating.LikeCount = Math.Max(0, rating.LikeCount - 1);
                else
                    rating.DislikeCount = Math.Max(0, rating.DislikeCount - 1);
            }
        }

        context.RatingReactions.Remove(existing);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RatingReactionRecord?> GetReactionAsync(EntityRef ratingRef, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(ratingRef, nameof(ratingRef));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.RatingReactions.FirstOrDefaultAsync(
                r => r.ForEntityType == ratingRef.EntityType && r.ForEntityId == ratingRef.EntityId && r.FromEntityType == fromEntity.EntityType &&
                    r.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToReactionRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RatingRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.Where(r => r.FromEntityType == fromEntity.EntityType && r.FromEntityId == fromEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RatingRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Ratings.Where(r => r.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(forEntityId))
            query = query.Where(r => r.ForEntityId == forEntityId);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Ratings.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            var ratingIdStr = id.ToString();
            var reactions = await context.RatingReactions.Where(r => r.ForEntityType == "Rating" && r.ForEntityId == ratingIdStr).ToListAsync(ct).ConfigureAwait(false);
            context.RatingReactions.RemoveRange(reactions);
            context.Ratings.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteForEntityFromEntityAsync(EntityRef forEntity, EntityRef fromEntity, string? subject = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings
            .Where(r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forEntity.EntityId && r.FromEntityType == fromEntity.EntityType &&
                r.FromEntityId == fromEntity.EntityId && r.Subject == subject)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var ratingIds = entities.Select(e => e.Id.ToString()).ToHashSet();
        var reactions = await context.RatingReactions.Where(r => r.ForEntityType == "Rating" && ratingIds.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.RatingReactions.RemoveRange(reactions);
        context.Ratings.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Ratings.Where(r => r.ForEntityType == forEntity.EntityType && r.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        var ratingIds = entities.Select(e => e.Id.ToString()).ToHashSet();
        var reactions = await context.RatingReactions.Where(r => r.ForEntityType == "Rating" && ratingIds.Contains(r.ForEntityId)).ToListAsync(ct).ConfigureAwait(false);
        context.RatingReactions.RemoveRange(reactions);
        context.Ratings.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static RatingRecord ToRecord(RatingEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            Subject = e.Subject,
            Title = e.Title,
            Value = e.Value,
            Message = e.Message,
            LikeCount = e.LikeCount,
            DislikeCount = e.DislikeCount,
            CreatedTimestamp = e.CreatedTimestamp,
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