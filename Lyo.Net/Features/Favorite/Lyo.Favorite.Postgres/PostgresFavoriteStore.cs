using System.Diagnostics;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;
using Lyo.Favorite.Postgres.Database;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Favorite.Postgres;

/// <summary>PostgreSQL implementation of IFavoriteStore.</summary>
public sealed class PostgresFavoriteStore : IFavoriteStore, IHealth
{
    private readonly IDbContextFactory<FavoriteDbContext> _contextFactory;

    /// <summary>Creates a new PostgresFavoriteStore.</summary>
    public PostgresFavoriteStore(IDbContextFactory<FavoriteDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task SaveAsync(FavoriteRecord favorite, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(favorite, nameof(favorite));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await context.Favorites.FirstOrDefaultAsync(
                f => f.ForEntityType == favorite.ForEntityType && f.ForEntityId == favorite.ForEntityId && f.FromEntityType == favorite.FromEntityType &&
                    f.FromEntityId == favorite.FromEntityId, ct)
            .ConfigureAwait(false);

        if (existing != null)
            return;

        var entity = new FavoriteEntity {
            Id = favorite.Id == default ? Guid.NewGuid() : favorite.Id,
            ForEntityType = favorite.ForEntityType,
            ForEntityId = favorite.ForEntityId,
            FromEntityType = favorite.FromEntityType,
            FromEntityId = favorite.FromEntityId,
            CreatedTimestamp = favorite.CreatedTimestamp == default ? DateTime.UtcNow : favorite.CreatedTimestamp
        };

        context.Favorites.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FavoriteRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Favorites.FindAsync([id], ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<FavoriteRecord?> GetAsync(EntityRef forEntity, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Favorites.FirstOrDefaultAsync(
                f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == forEntity.EntityId && f.FromEntityType == fromEntity.EntityType &&
                    f.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<bool> IsFavoritedAsync(EntityRef forEntity, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Favorites.AnyAsync(
                f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == forEntity.EntityId && f.FromEntityType == fromEntity.EntityType &&
                    f.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Favorites.Where(f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == forEntity.EntityId)
            .OrderBy(f => f.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteRecord>> GetFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Favorites.Where(f => f.FromEntityType == fromEntity.EntityType && f.FromEntityId == fromEntity.EntityId)
            .OrderBy(f => f.CreatedTimestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FavoriteRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Favorites.Where(f => f.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(forEntityId))
            query = query.Where(f => f.ForEntityId == forEntityId);

        var entities = await query.OrderBy(f => f.CreatedTimestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetCountForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.Favorites.CountAsync(f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == forEntity.EntityId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Favorites.FindAsync([id], ct).ConfigureAwait(false);
        if (entity != null) {
            context.Favorites.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(EntityRef forEntity, EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Favorites.FirstOrDefaultAsync(
                f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == forEntity.EntityId && f.FromEntityType == fromEntity.EntityType &&
                    f.FromEntityId == fromEntity.EntityId, ct)
            .ConfigureAwait(false);

        if (entity != null) {
            context.Favorites.Remove(entity);
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Favorites.Where(f => f.ForEntityType == forEntity.EntityType && f.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        context.Favorites.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteFromEntityAsync(EntityRef fromEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(fromEntity, nameof(fromEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Favorites.Where(f => f.FromEntityType == fromEntity.EntityType && f.FromEntityId == fromEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        context.Favorites.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "favorite-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
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

    private static FavoriteRecord ToRecord(FavoriteEntity e)
        => new() {
            Id = e.Id,
            ForEntityType = e.ForEntityType,
            ForEntityId = e.ForEntityId,
            FromEntityType = e.FromEntityType,
            FromEntityId = e.FromEntityId,
            CreatedTimestamp = e.CreatedTimestamp
        };
}