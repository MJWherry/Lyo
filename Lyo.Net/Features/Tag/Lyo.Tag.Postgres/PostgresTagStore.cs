using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Tag.Postgres.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lyo.Tag.Postgres;

/// <summary>PostgreSQL implementation of ITagStore.</summary>
public sealed class PostgresTagStore : EntityRefPostgresStoreBase, ITagStore, IHealth
{
    private const string ModuleKey = "Tag";

    private readonly IDbContextFactory<TagDbContext> _contextFactory;

    /// <summary>Creates a new PostgresTagStore.</summary>
    public PostgresTagStore(
        IDbContextFactory<TagDbContext> contextFactory,
        IOptions<EntityRefOptions> entityRefOptions,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
        : base(entityRefOptions, interceptors)
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
        var forEntityId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(null);
        var actor = fromEntity ?? EntityRef.ForGuid(EntityRefWellKnown.SystemActorType, EntityRefWellKnown.SystemActorId);
        var fromEntityId = EntityRefPersistedGuid.RequirePersistedGuid(actor);
        var slugNormalized = NormalizeSlug(slug);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var exists = await context.Tags.WhereActive().WhereTenant(resolvedTenant).AnyAsync(
                t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntityId && t.Name == tag && t.TagType == tagType && t.Slug == slugNormalized, ct)
            .ConfigureAwait(false);

        if (exists)
            return;

        var entity = new TagEntity {
            Id = Guid.NewGuid(),
            ForEntityType = forEntity.EntityType,
            ForEntityId = forEntityId,
            FromEntityType = actor.EntityType,
            FromEntityId = fromEntityId,
            TenantId = resolvedTenant,
            Name = tag,
            TagType = tagType,
            Slug = slugNormalized,
            Visibility = EntityRefVisibility.Private
        };

        await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.BeforePersist, entity, ct).ConfigureAwait(false);
        context.Tags.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.AfterPersist, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveTagAsync(EntityRef forEntity, string tag, string tagType = "tag", string? slug = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tagType);
        var forEntityId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(null);
        var slugNormalized = NormalizeSlug(slug);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Tags.WhereActive().WhereTenant(resolvedTenant)
            .Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntityId && t.Name == tag && t.TagType == tagType && t.Slug == slugNormalized)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.BeforeSoftDelete, e, ct).ConfigureAwait(false);

        foreach (var e in entities)
            e.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.AfterSoftDelete, e, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetTagsForEntityAsync(EntityRef forEntity, string? tagType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forEntityId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(null);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.WhereActive().WhereTenant(resolvedTenant).Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntityId);
        if (!string.IsNullOrWhiteSpace(tagType))
            query = query.Where(t => t.TagType == tagType);

        var entities = await query.OrderBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TagRecord>> GetEntitiesWithTagAsync(string tag, string? forEntityType = null, string? tagType = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(tag);
        var resolvedTenant = ResolveTenant(null);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.WhereActive().WhereTenant(resolvedTenant).Where(t => t.Name == tag);
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
        var resolvedTenant = ResolveTenant(null);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Tags.WhereActive().WhereTenant(resolvedTenant).Where(t => t.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(tagType))
            query = query.Where(t => t.TagType == tagType);

        return await query.Select(t => t.Name).Distinct().OrderBy(t => t).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAllTagsForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var forEntityId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(null);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Tags.WhereActive().WhereTenant(resolvedTenant).Where(t => t.ForEntityType == forEntity.EntityType && t.ForEntityId == forEntityId).ToListAsync(ct).ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.BeforeSoftDelete, e, ct).ConfigureAwait(false);

        foreach (var e in entities)
            e.DeletedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        foreach (var e in entities)
            await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.AfterSoftDelete, e, ct).ConfigureAwait(false);
    }

    private static TagRecord ToRecord(TagEntity e)
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
            Name = e.Name,
            TagType = e.TagType,
            Slug = e.Slug
        };

    private static string NormalizeSlug(string? slug) => string.IsNullOrWhiteSpace(slug) ? string.Empty : slug.Trim();
}
