using System.Text.Json;
using Lyo.ChangeTracker.Postgres.Database;
using Lyo.Common.Core.Extensions;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.ChangeTracker.Postgres;

/// <summary>An <see cref="IChangeTracker" /> that persists change rows in PostgreSQL.</summary>
public sealed class PostgresChangeTracker : IChangeTracker, IHealth
{
    private readonly IDbContextFactory<ChangeTrackerDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    public PostgresChangeTracker(
        IDbContextFactory<ChangeTrackerDbContext> contextFactory,
        EntityRefOptions entityRefOptions,
        PostgresChangeTrackerOptions changeTrackerOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(changeTrackerOptions);
        _contextFactory = contextFactory;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = changeTrackerOptions.Tenancy;
    }

    /// <inheritdoc />
    public void RecordChange(ChangeRecord change)
    {
        ArgumentHelpers.ThrowIfNull(change);
        RecordChanges([change]);
    }

    /// <inheritdoc />
    public async Task RecordChangeAsync(ChangeRecord change, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(change);
        await RecordChangesAsync([change], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordChanges(IEnumerable<ChangeRecord> changes)
    {
        ArgumentHelpers.ThrowIfNull(changes);
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
        ArgumentHelpers.ThrowIfNull(changes);
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
        ArgumentHelpers.ThrowIfNull(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Changes.Where(c => c.SubjectEntityType == forEntity.EntityType && c.SubjectEntityId == forEntity.EntityId)
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChangeRecord>> GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(forEntityType);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = context.Changes.Where(c => c.SubjectEntityType == forEntityType);
        if (!string.IsNullOrWhiteSpace(forEntityId))
            query = query.Where(c => c.SubjectEntityId == forEntityId);

        var entities = await query.OrderByDescending(c => c.Timestamp).ToListAsync(ct).ConfigureAwait(false);
        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task DeleteForEntityAsync(EntityRef forEntity, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(forEntity);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await context.Changes.Where(c => c.SubjectEntityType == forEntity.EntityType && c.SubjectEntityId == forEntity.EntityId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (entities.Count == 0)
            return;

        context.Changes.RemoveRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "change-tracker-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_contextFactory, PostgresChangeTrackerOptions.Schema, ct);

    private ChangeEntryEntity ToEntity(ChangeRecord record)
    {
        ArgumentHelpers.ThrowIfNull(record.ForEntity, nameof(record.ForEntity));
        return new() {
            Id = record.Id,
            Timestamp = record.Timestamp,
            SubjectEntityType = record.ForEntity.EntityType,
            SubjectEntityId = record.ForEntity.EntityId,
            ActorEntityType = record.FromEntity?.EntityType,
            ActorEntityId = record.FromEntity?.EntityId,
            TenantId = TenancyResolver.Resolve(record.TenantId, _featureTenancy, _entityRefOptions),
            ChangeType = record.ChangeType,
            Message = record.Message,
            OldValuesJson = SerializeDict(record.OldValues),
            ChangedPropertiesJson = SerializeDict(record.ChangedProperties)
        };
    }

    private static ChangeRecord ToRecord(ChangeEntryEntity entity)
        => new(new(entity.SubjectEntityType!, entity.SubjectEntityId!), DeserializeDict(entity.OldValuesJson), DeserializeDict(entity.ChangedPropertiesJson)) {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            FromEntity = !entity.ActorEntityType.IsNullOrWhitespace() && !entity.ActorEntityId.IsNullOrWhitespace()
                ? new EntityRef(entity.ActorEntityType!, entity.ActorEntityId!)
                : null,
            TenantId = entity.TenantId,
            ChangeType = entity.ChangeType,
            Message = entity.Message
        };

    private static string SerializeDict(IReadOnlyDictionary<string, object?>? dict) => dict == null || dict.Count == 0 ? "{}" : JsonSerializer.Serialize(dict);

    private static IReadOnlyDictionary<string, object?> DeserializeDict(string? json)
        => string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new Dictionary<string, object?>();
}