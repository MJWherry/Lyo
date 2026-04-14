using System.Diagnostics;
using System.Text.Json;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.ChangeTracker.Postgres;

/// <summary>PostgreSQL implementation of IChangeTracker.</summary>
public sealed class PostgresChangeTracker : IChangeTracker, IHealth
{
    private readonly IDbContextFactory<ChangeTrackerDbContext> _contextFactory;

    public PostgresChangeTracker(IDbContextFactory<ChangeTrackerDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public void RecordChange(ChangeRecord change)
    {
        ArgumentHelpers.ThrowIfNull(change, nameof(change));
        RecordChanges([change]);
    }

    /// <inheritdoc />
    public async Task RecordChangeAsync(ChangeRecord change, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(change, nameof(change));
        await RecordChangesAsync([change], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordChanges(IEnumerable<ChangeRecord> changes)
    {
        ArgumentHelpers.ThrowIfNull(changes, nameof(changes));
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        using var context = _contextFactory.CreateDbContext();
        context.Changes.AddRange(list.Select(ToEntity));
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RecordChangesAsync(IEnumerable<ChangeRecord> changes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(changes, nameof(changes));
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.Changes.AddRange(list.Select(ToEntity));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ChangeRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Changes.SingleOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChangeRecord>> GetForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Changes.Where(c => c.ForEntityType == forEntity.EntityType && c.ForEntityId == forEntity.EntityId)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChangeRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType, nameof(forEntityType));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Changes.Where(c => c.ForEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(forEntityId))
            query = query.Where(c => c.ForEntityId == forEntityId);

        var entities = await query.OrderByDescending(c => c.Timestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity, nameof(forEntity));
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Changes.Where(c => c.ForEntityType == forEntity.EntityType && c.ForEntityId == forEntity.EntityId).ToListAsync(ct).ConfigureAwait(false);
        if (entities.Count == 0)
            return;

        context.Changes.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "change-tracker-postgres";

    /// <inheritdoc />
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = PostgresChangeTrackerOptions.Schema })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static ChangeEntryEntity ToEntity(ChangeRecord record)
    {
        ArgumentHelpers.ThrowIfNull(record.ForEntity, nameof(record.ForEntity));
        return new() {
            Id = record.Id,
            Timestamp = record.Timestamp,
            ForEntityType = record.ForEntity.EntityType,
            ForEntityId = record.ForEntity.EntityId,
            FromEntityType = record.FromEntity?.EntityType,
            FromEntityId = record.FromEntity?.EntityId,
            ChangeType = record.ChangeType,
            Message = record.Message,
            OldValuesJson = SerializeDict(record.OldValues),
            ChangedPropertiesJson = SerializeDict(record.ChangedProperties)
        };
    }

    private static ChangeRecord ToRecord(ChangeEntryEntity entity)
        => new(new(entity.ForEntityType, entity.ForEntityId), DeserializeDict(entity.OldValuesJson), DeserializeDict(entity.ChangedPropertiesJson)) {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            FromEntity = !string.IsNullOrWhiteSpace(entity.FromEntityType) && !string.IsNullOrWhiteSpace(entity.FromEntityId)
                ? new EntityRef(entity.FromEntityType, entity.FromEntityId)
                : null,
            ChangeType = entity.ChangeType,
            Message = entity.Message
        };

    private static string SerializeDict(IReadOnlyDictionary<string, object?>? dict) => dict == null || dict.Count == 0 ? "{}" : JsonSerializer.Serialize(dict);

    private static IReadOnlyDictionary<string, object?> DeserializeDict(string? json)
        => string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
}