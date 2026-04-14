using System.Diagnostics;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Tag.Postgres;

/// <summary>PostgreSQL implementation of ITagStore.</summary>
public sealed class PostgresTagStore : ITagStore, IHealth
{
    private readonly IDbContextFactory<TagDbContext> _contextFactory;

    /// <summary>Creates a new PostgresTagStore.</summary>
    public PostgresTagStore(IDbContextFactory<TagDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "tag-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "tag" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    /// <inheritdoc />
    public async Task AddTagAsync(EntityRef forEntity, string tag, EntityRef? fromEntity = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag, nameof(tag));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var exists = await context.Tags.AnyAsync(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId && t.Tag == tag, ct).ConfigureAwait(false);
        if (exists)
            return;

        var entity = new TagEntity {
            Id = Guid.NewGuid(),
            ForEntityType = forEntity.EntityType,
            ForEntityId = forEntity.EntityId,
            Tag = tag,
            FromEntityType = fromEntity?.EntityType,
            FromEntityId = fromEntity?.EntityId,
            CreatedTimestamp = DateTime.UtcNow
        };

        context.Tags.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveTagAsync(EntityRef forEntity, string tag, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag, nameof(tag));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Tags.Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId && t.Tag == tag)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        context.Tags.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Tags.Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId)
            .OrderBy(t => t.Tag)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag, nameof(tag));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.Where(t => t.Tag == tag);
        if (!string.IsNullOrWhiteSpace(forEntityType))
            query = query.Where(t => t.ForEntityType == forEntityType);

        var entities = await query.OrderBy(t => t.ForEntityType).ThenBy(t => t.ForEntityId).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Tags.Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        context.Tags.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static TagRecord ToRecord(TagEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            Tag = e.Tag,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            CreatedTimestamp = e.CreatedTimestamp
        };
}