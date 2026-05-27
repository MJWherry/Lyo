using System.Diagnostics;
using System.Text.Json;
using Lyo.Audit.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Health;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Audit.Postgres;

/// <summary>PostgreSQL implementation of IAuditRecorder that persists audit entries to the database.</summary>
public sealed class PostgresAuditRecorder : IAuditRecorder, IHealth
{
    private readonly IDbContextFactory<AuditDbContext> _contextFactory;

    /// <summary>Creates a new PostgresAuditRecorder.</summary>
    /// <param name="contextFactory">Factory for creating AuditDbContext instances</param>
    public PostgresAuditRecorder(IDbContextFactory<AuditDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
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
    public async Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try {
            await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var canConnect = await context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            sw.Stop();
            return canConnect
                ? HealthResult.Healthy(sw.Elapsed, null, new Dictionary<string, object?> { ["database"] = "audit" })
                : HealthResult.Unhealthy(sw.Elapsed, "Database connection failed");
        }
        catch (Exception ex) {
            sw.Stop();
            return HealthResult.Unhealthy(sw.Elapsed, ex.Message, null, ex);
        }
    }

    private static AuditChangeEntity ToEntity(AuditChange change)
        => new() {
            Id = change.Id,
            Timestamp = change.Timestamp,
            ForEntityType = change.Entity.EntityType,
            ForEntityId = change.Entity.EntityId,
            FromEntityType = change.Actor?.EntityType,
            FromEntityId = change.Actor?.EntityId,
            OldValuesJson = SerializeDict(change.OldValues),
            ChangedPropertiesJson = SerializeDict(change.ChangedProperties)
        };

    private static AuditEventEntity ToEntity(AuditEvent evt)
        => new() {
            Id = evt.Id,
            EventType = evt.EventType,
            Timestamp = evt.Timestamp,
            ForEntityType = evt.Subject.EntityType,
            ForEntityId = evt.Subject.EntityId,
            FromEntityType = evt.Actor?.EntityType,
            FromEntityId = evt.Actor?.EntityId,
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
