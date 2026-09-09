using System.Text.Json;
using Lyo.Audit.Postgres.Database;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Audit.Postgres;

/// <summary>An <see cref="IAuditRecorder" /> that writes audit rows into PostgreSQL.</summary>
public sealed class PostgresAuditRecorder : IAuditRecorder, IHealth
{
    private readonly IDbContextFactory<AuditDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    /// <summary>Builds a recorder backed by <see cref="AuditDbContext" />.</summary>
    /// <param name="contextFactory">Creates <see cref="AuditDbContext" /> instances.</param>
    /// <param name="entityRefOptions">Process-wide EntityRef settings (default tenant and mode).</param>
    /// <param name="auditOptions">Feature-level audit settings (includes the audit <see cref="TenancyOptions" />).</param>
    public PostgresAuditRecorder(IDbContextFactory<AuditDbContext> contextFactory, EntityRefOptions entityRefOptions, PostgresAuditOptions auditOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(auditOptions);
        _contextFactory = contextFactory;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = auditOptions.Tenancy;
    }

    /// <inheritdoc />
    public void RecordChange(AuditChange change)
    {
        ArgumentHelpers.ThrowIfNull(change);
        RecordChanges([change]);
    }

    /// <inheritdoc />
    public async Task RecordChangeAsync(AuditChange change, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(change);
        await RecordChangesAsync([change], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordChanges(IEnumerable<AuditChange> changes)
    {
        ArgumentHelpers.ThrowIfNull(changes);
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        using var context = _contextFactory.CreateDbContext();
        context.AuditChanges.AddRange(list.Select(ToEntity));
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RecordChangesAsync(IEnumerable<AuditChange> changes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(changes);
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.AuditChanges.AddRange(list.Select(ToEntity));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordEvent(AuditEvent evt)
    {
        ArgumentHelpers.ThrowIfNull(evt);
        RecordEvents([evt]);
    }

    /// <inheritdoc />
    public async Task RecordEventAsync(AuditEvent evt, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(evt);
        await RecordEventsAsync([evt], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordEvents(IEnumerable<AuditEvent> events)
    {
        ArgumentHelpers.ThrowIfNull(events);
        var list = events.ToList();
        if (list.Count == 0)
            return;

        using var context = _contextFactory.CreateDbContext();
        context.AuditEvents.AddRange(list.Select(ToEntity));
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RecordEventsAsync(IEnumerable<AuditEvent> events, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(events);
        var list = events.ToList();
        if (list.Count == 0)
            return;

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.AuditEvents.AddRange(list.Select(ToEntity));
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public string HealthCheckName => "audit-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_contextFactory, PostgresAuditOptions.Schema, ct);

    private AuditChangeEntity ToEntity(AuditChange change)
        => new() {
            Id = change.Id,
            Timestamp = change.Timestamp,
            SubjectEntityType = change.Entity.EntityType,
            SubjectEntityId = change.Entity.EntityId,
            ActorEntityType = change.Actor?.EntityType,
            ActorEntityId = change.Actor?.EntityId,
            TenantId = TenancyResolver.Resolve(change.TenantId, _featureTenancy, _entityRefOptions),
            OldValuesJson = SerializeDict(change.OldValues),
            ChangedPropertiesJson = SerializeDict(change.ChangedProperties)
        };

    private AuditEventEntity ToEntity(AuditEvent evt)
        => new() {
            Id = evt.Id,
            EventType = evt.EventType,
            Timestamp = evt.Timestamp,
            SubjectEntityType = evt.Subject.EntityType,
            SubjectEntityId = evt.Subject.EntityId,
            ActorEntityType = evt.Actor?.EntityType,
            ActorEntityId = evt.Actor?.EntityId,
            TenantId = TenancyResolver.Resolve(evt.TenantId, _featureTenancy, _entityRefOptions),
            Message = evt.Message,
            MetadataJson = evt.Metadata is { Count: > 0 } ? SerializeDict(evt.Metadata) : null
        };

    private static string SerializeDict(IReadOnlyDictionary<string, object?>? dict)
    {
        if (dict == null || dict.Count == 0)
            return "{}";

        var stringDict = new Dictionary<string, object?>();
        foreach (var kvp in dict)
            stringDict[kvp.Key] = kvp.Value;

        return JsonSerializer.Serialize(stringDict);
    }
}