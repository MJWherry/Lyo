# Lyo.Audit

Audit trail library with two records: `AuditChange` (entity change tracking) and `AuditEvent` (events to log). Both are immutable. Once created they do not change. Each carries an `EntityRef` for the subject plus an optional `EntityRef` for the actor. `IAuditRecorder` is the storage contract.

## Features

- **AuditChange.** Entity/property change record: `Id` (Guid), `Timestamp`, `Entity` (`EntityRef`), `OldValues` (property to old value), `ChangedProperties` (property to new value), optional `Actor` (`EntityRef?`).
- **AuditEvent.** Event to log: `Id` (Guid), `Subject` (`EntityRef`), `EventType`, `Timestamp`, optional `Message`, `Actor` (`EntityRef?`), and `Metadata`.
- **IAuditRecorder.** Sync and async methods: `RecordChange`/`RecordChangeAsync`, `RecordChanges`/`RecordChangesAsync`, `RecordEvent`/`RecordEventAsync`, `RecordEvents`/`RecordEventsAsync`. Implement to persist to a database or log sink.
- **NullAuditRecorder.** No-op implementation when auditing is not needed.

## Examples

### AuditChange (entity changes)

```csharp
using Lyo.Audit;
using Lyo.EntityReference.Models;

var change = new AuditChange(
    EntityRef.For<Order>(order.Id),
    new Dictionary<string, object?> {
        ["Name"] = "Old Name",
        ["Status"] = "Draft"
    },
    new Dictionary<string, object?> {
        ["Name"] = "New Name",
        ["Status"] = "Submitted"
    }) {
    Actor = EntityRef.ForKey("User", currentUserId.ToString())
};

auditRecorder.RecordChange(change);
```

### AuditEvent (events to log)

```csharp
using Lyo.Audit;
using Lyo.EntityReference.Models;

var evt = new AuditEvent(
    Subject: EntityRef.ForKey("User", "user-123"),
    EventType: "UserLogin",
    Message: "User signed in successfully",
    Actor: EntityRef.ForKey("User", "user-123"),
    Metadata: new Dictionary<string, object?> {
        ["IpAddress"] = "192.168.1.1",
        ["UserAgent"] = "Mozilla/5.0..."
    });

auditRecorder.RecordEvent(evt);
```

### Bulk and async recording

```csharp
auditRecorder.RecordChanges(new[] { change1, change2, change3 });
auditRecorder.RecordEvents(new[] { evt1, evt2 });

await auditRecorder.RecordChangeAsync(change);
await auditRecorder.RecordChangesAsync(changes, cancellationToken);
await auditRecorder.RecordEventAsync(evt);
await auditRecorder.RecordEventsAsync(events, cancellationToken);
```

## AuditChange (entity changes)

Decorate domain types with `[EntityRefLogicalType("MyApp.Order")]` to keep the persisted `EntityType` stable across CLR renames.

## PostgreSQL persistence

Use `Lyo.Audit.Postgres` for PostgreSQL storage with EF Core migrations:

```xml
<PackageReference Include="Lyo.Audit.Postgres" Version="1.0.22" />
```

```csharp
services.AddPostgresAuditRecorder(new PostgresAuditOptions {
    ConnectionString = configuration.GetConnectionString("Audit"),
    EnableAutoMigrations = true
});
```

When `EnableAutoMigrations` is true, migrations run at host startup via `IHostedService`, not during service registration. The app must use a host such as
`Host.CreateDefaultBuilder()` or `WebApplication.CreateBuilder()`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)