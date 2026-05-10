using System.Diagnostics;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Favorite.Postgres.Database;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lyo.Favorite.Postgres;

/// <summary>PostgreSQL implementation of <see cref="IFavoriteStore"/>.</summary>
public sealed class PostgresFavoriteStore : EntityRefPostgresStoreBase, IFavoriteStore, IHealth
{
    private const int BatchQueryChunkSize = 500;
    private const string ModuleKey = "Favorite";

    private readonly IDbContextFactory<FavoriteDbContext> _contextFactory;

    /// <summary>Creates a new PostgresFavoriteStore.</summary>
    public PostgresFavoriteStore(
        IDbContextFactory<FavoriteDbContext> contextFactory,
        IOptions<EntityRefOptions> entityRefOptions,
        IEnumerable<IEntityRefActionInterceptor>? interceptors = null)
        : base(entityRefOptions, interceptors)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task SaveAsync(FavoriteRecord favorite, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(favorite);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var targetId = favorite.ForEntityId != default ? favorite.ForEntityId : throw new ArgumentException("FavoriteRecord.ForEntityId is required.", nameof(favorite));
        var appliedId = favorite.FromEntityId != default ? favorite.FromEntityId : throw new ArgumentException("FavoriteRecord.FromEntityId is required.", nameof(favorite));

        var existing = await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant)
            .FirstOrDefaultAsync(
                f => f.ForEntityType == favorite.ForEntityType && f.ForEntityId == targetId && f.FromEntityType == favorite.FromEntityType && f.FromEntityId == appliedId &&
                    (context == null ? f.Context == null : f.Context == context), ct)
            .ConfigureAwait(false);

        if (existing != null)
            return;

        var entity = new FavoriteEntity {
            Id = favorite.Id == default ? Guid.NewGuid() : favorite.Id,
            ForEntityType = favorite.ForEntityType,
            ForEntityId = targetId,
            FromEntityType = favorite.FromEntityType,
            FromEntityId = appliedId,
            TenantId = resolvedTenant,
            Context = context,
            ExpiresAt = favorite.ExpiresAt,
            MetadataJson = favorite.MetadataJson,
            Visibility = string.IsNullOrWhiteSpace(favorite.Visibility) ? EntityRefVisibility.Private : favorite.Visibility,
            CreatedAt = favorite.CreatedAt == default ? DateTime.UtcNow : favorite.CreatedAt
        };

        await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.BeforePersist, entity, ct).ConfigureAwait(false);

        contextDb.Favorites.Add(entity);
        await contextDb.SaveChangesAsync(ct).ConfigureAwait(false);

        await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.AfterPersist, entity, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteRecord?> GetByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<FavoriteRecord?> GetAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var targetId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var appliedId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant)
            .FirstOrDefaultAsync(
                f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == targetId && f.FromEntityType == fromEntity.EntityType && f.FromEntityId == appliedId &&
                    (context == null ? f.Context == null : f.Context == context), ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<bool> IsFavoritedAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var targetId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var appliedId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant)
            .AnyAsync(
                f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == targetId && f.FromEntityType == fromEntity.EntityType && f.FromEntityId == appliedId &&
                    (context == null ? f.Context == null : f.Context == context), ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteRecord>> GetForEntityAsync(EntityRef forEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var targetId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == targetId);
        if (context != null)
            query = query.Where(f => f.Context == context);

        var entities = await query.OrderBy(f => f.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteRecord>> GetFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var appliedId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.FromEntityType == fromEntity.EntityType && f.FromEntityId == appliedId);
        if (context != null)
            query = query.Where(f => f.Context == context);

        var entities = await query.OrderBy(f => f.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteRecord>> GetForEntityTypeAsync(string forEntityType, Guid? forEntityId = null, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.ForEntityType == forEntityType);
        if (forEntityId.HasValue)
            query = query.Where(f => f.ForEntityId == forEntityId.Value);

        var entities = await query.OrderBy(f => f.CreatedAt).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetCountForEntityAsync(EntityRef forEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var targetId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == targetId);
        if (context != null)
            query = query.Where(f => f.Context == context);

        return await query.CountAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetFavoriteCountsForEntitiesAsync(string forEntityType, IReadOnlyList<Guid> forEntityIds, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        ArgumentHelpers.ThrowIfNull(forEntityIds);
        if (forEntityIds.Count == 0)
            return new Dictionary<Guid, int>();

        var distinctIds = forEntityIds.Distinct().ToArray();
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        contextDb.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        var result = new Dictionary<Guid, int>();
        foreach (var chunk in distinctIds.Chunk(BatchQueryChunkSize)) {
            var ids = chunk.ToArray();
            var rows = await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.ForEntityType == forEntityType && ids.Contains(f.ForEntityId))
                .GroupBy(f => f.ForEntityId)
                .Select(g => new { ForEntityId = g.Key, Count = g.Count() })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var row in rows)
                result[row.ForEntityId] = row.Count;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
        if (entity == null)
            return;

        await SoftDeleteAsync(contextDb, entity, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(EntityRef forEntity, EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var targetId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var appliedId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant)
            .FirstOrDefaultAsync(
                f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == targetId && f.FromEntityType == fromEntity.EntityType && f.FromEntityId == appliedId &&
                    (context == null ? f.Context == null : f.Context == context), ct)
            .ConfigureAwait(false);

        if (entity == null)
            return;

        await SoftDeleteAsync(contextDb, entity, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        var targetId = EntityRefPersistedGuid.RequirePersistedGuid(forEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == targetId);
        if (context != null)
            query = query.Where(f => f.Context == context);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);
        foreach (var entity in entities)
            await SoftDeleteAsync(contextDb, entity, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteFromEntityAsync(EntityRef fromEntity, Guid? tenantId = null, string? context = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity);
        var appliedId = EntityRefPersistedGuid.RequirePersistedGuid(fromEntity);
        var resolvedTenant = ResolveTenant(tenantId);

        await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = contextDb.Favorites.WhereActive().WhereTenant(resolvedTenant).Where(f => f.FromEntityType == fromEntity.EntityType && f.FromEntityId == appliedId);
        if (context != null)
            query = query.Where(f => f.Context == context);

        var entities = await query.ToListAsync(ct).ConfigureAwait(false);
        foreach (var entity in entities)
            await SoftDeleteAsync(contextDb, entity, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "favorite-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var contextDb = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await contextDb.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "favorite" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private async Task SoftDeleteAsync(FavoriteDbContext contextDb, FavoriteEntity entity, Guid resolvedTenant, CancellationToken ct)
    {
        await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.BeforeSoftDelete, entity, ct).ConfigureAwait(false);

        entity.DeletedAt = DateTime.UtcNow;
        await contextDb.SaveChangesAsync(ct).ConfigureAwait(false);

        await RunInterceptorsAsync(ModuleKey, resolvedTenant, EntityRefActionKind.AfterSoftDelete, entity, ct).ConfigureAwait(false);
    }

    private static FavoriteRecord ToRecord(FavoriteEntity e)
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
            Visibility = e.Visibility
        };
}
