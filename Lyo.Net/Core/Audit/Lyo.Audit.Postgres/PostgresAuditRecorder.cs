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
        ArgumentHelpers.ThrowIfNull(contextFactory, nameof(contextFactory));
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public void RecordChange(AuditChange change)
    {
        ArgumentHelpers.ThrowIfNull(change, nameof(change));
        RecordChanges([change]);
    }

    /// <inheritdoc />
    public async Task RecordChangeAsync(AuditChange change, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(change, nameof(change));
        await RecordChangesAsync([change], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordChanges(IEnumerable<AuditChange> changes)
    {
        ArgumentHelpers.ThrowIfNull(changes, nameof(changes));
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        var entities = list.Select(c => new AuditChangeEntity {
                Id = c.Id,
                Timestamp = c.Timestamp,
                TypeAssemblyFullName = c.TypeAssemblyFullName,
                OldValuesJson = SerializeDict(c.OldValues),
                ChangedPropertiesJson = SerializeDict(c.ChangedProperties)
            })
            .ToList();

        using var context = _contextFactory.CreateDbContext();
        context.AuditChanges.AddRange(entities);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RecordChangesAsync(IEnumerable<AuditChange> changes, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(changes, nameof(changes));
        var list = changes.ToList();
        if (list.Count == 0)
            return;

        var entities = list.Select(c => new AuditChangeEntity {
                Id = c.Id,
                Timestamp = c.Timestamp,
                TypeAssemblyFullName = c.TypeAssemblyFullName,
                OldValuesJson = SerializeDict(c.OldValues),
                ChangedPropertiesJson = SerializeDict(c.ChangedProperties)
            })
            .ToList();

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.AuditChanges.AddRange(entities);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordEvent(AuditEvent evt)
    {
        ArgumentHelpers.ThrowIfNull(evt, nameof(evt));
        RecordEvents([evt]);
    }

    /// <inheritdoc />
    public async Task RecordEventAsync(AuditEvent evt, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(evt, nameof(evt));
        await RecordEventsAsync([evt], ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void RecordEvents(IEnumerable<AuditEvent> events)
    {
        ArgumentHelpers.ThrowIfNull(events, nameof(events));
        var list = events.ToList();
        if (list.Count == 0)
            return;

        var entities = list.Select(e => new AuditEventEntity {
                Id = e.Id,
                EventType = e.EventType,
                Timestamp = e.Timestamp,
                Message = e.Message,
                Actor = e.Actor,
                MetadataJson = e.Metadata != null && e.Metadata.Count > 0 ? SerializeDict(e.Metadata) : null
            })
            .ToList();

        using var context = _contextFactory.CreateDbContext();
        context.AuditEvents.AddRange(entities);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RecordEventsAsync(IEnumerable<AuditEvent> events, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(events, nameof(events));
        var list = events.ToList();
        if (list.Count == 0)
            return;

        var entities = list.Select(e => new AuditEventEntity {
                Id = e.Id,
                EventType = e.EventType,
                Timestamp = e.Timestamp,
                Message = e.Message,
                Actor = e.Actor,
                MetadataJson = e.Metadata != null && e.Metadata.Count > 0 ? SerializeDict(e.Metadata) : null
            })
            .ToList();

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        context.AuditEvents.AddRange(entities);
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