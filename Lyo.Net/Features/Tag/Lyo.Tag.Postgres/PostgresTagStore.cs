using System.Diagnostics;
using Lyo.Common.Identifiers;
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
        ArgumentHelpers.ThrowIfNull(contextFactory);
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
    public async Task AddTagAsync(EntityRef forEntity, string tag, string tagType = "tag", EntityRef? fromEntity = null, string? slug = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tagType);
        var slugNormalized = NormalizeSlug(slug);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var exists = await context.Tags.AnyAsync(
                t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId && t.Name == tag && t.TagType == tagType && t.Slug == slugNormalized, ct)
            .ConfigureAwait(false);

        if (exists)
            return;

        var entity = new TagEntity {
            Id = Guid.NewGuid(),
            ForEntityType = forEntity.EntityType,
            ForEntityId = forEntity.EntityId,
            Name = tag,
            TagType = tagType,
            Slug = slugNormalized,
            FromEntityType = fromEntity?.EntityType,
            FromEntityId = fromEntity?.EntityId,
            CreatedTimestamp = DateTime.UtcNow
        };

        context.Tags.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tagType);
        var slugNormalized = NormalizeSlug(slug);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Tags
            .Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId && t.Name == tag && t.TagType == tagType && t.Slug == slugNormalized)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        context.Tags.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntity.EntityId);
        if (!string.IsNullOrWhiteSpace(tagType))
            query = query.Where(t => t.TagType == tagType);

        var entities = await query.OrderBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.Where(t => t.Name == tag);
        if (!string.IsNullOrWhiteSpace(forEntityType))
            query = query.Where(t => t.ForEntityType == forEntityType);

        if (!string.IsNullOrWhiteSpace(tagType))
            query = query.Where(t => t.TagType == tagType);

        var entities = await query.OrderBy(t => t.ForEntityType).ThenBy(t => t.ForEntityId).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAllTagsForEntityTypeAsync(string forEntityType, string? tagType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.Where(t => t.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(tagType))
            query = query.Where(t => t.TagType == tagType);

        return await query.Select(t => t.Name).Distinct().OrderBy(t => t).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
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
            Name = e.Name,
            TagType = e.TagType,
            Slug = e.Slug,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            CreatedTimestamp = e.CreatedTimestamp
        };

    private static string NormalizeSlug(string? slug) => string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim();
}