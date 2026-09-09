# Lyo.Audit

Audit trail with two records: `AuditChange` (entity change tracking) and `AuditEvent` (events to log). Both are immutable and stay unchanged after creation. Each carries an `EntityRef` for the subject plus an optional `EntityRef` for the actor. Storage is contracted through `IAuditRecorder`.

## Features

- **AuditChange.** A change to an entity or property: `Id` (Guid), `Timestamp`, `Entity` (`EntityRef`), `OldValues` (property to old value), `ChangedProperties` (property to new value), optional `Actor` (`EntityRef?`).
- **AuditEvent.** Something to log: `Id` (Guid), `Subject` (`EntityRef`), `EventType`, `Timestamp`, optional `Message`, `Actor` (`EntityRef?`), and `Metadata`.
- **IAuditRecorder.** Sync and async: `RecordChange`/`RecordChangeAsync`, `RecordChanges`/`RecordChangesAsync`, `RecordEvent`/`RecordEventAsync`, `RecordEvents`/`RecordEventsAsync`. Persist to a database or log sink in your implementation.
- **NullAuditRecorder.** A no-op when you do not need auditing.

## Examples

### AuditChange (tracking entity edits)

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

### AuditEvent (events worth logging)

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

### Recording in bulk and asynchronously

```csharp
auditRecorder.RecordChanges(new[] { change1, change2, change3 });
auditRecorder.RecordEvents(new[] { evt1, evt2 });

await auditRecorder.RecordChangeAsync(change);
await auditRecorder.RecordChangesAsync(changes, cancellationToken);
await auditRecorder.RecordEventAsync(evt);
await auditRecorder.RecordEventsAsync(events, cancellationToken);
```

## AuditChange (tracking entity edits)

Decorate domain types with `[EntityRefLogicalType("MyApp.Order")]` so the persisted `EntityType` stays stable if the CLR type is renamed.

## Storing in PostgreSQL

PostgreSQL storage with EF Core migrations is in `Lyo.Audit.Postgres`:

```xml
<PackageReference Include="Lyo.Audit.Postgres" Version="1.0.22" />
```

```csharp
services.AddPostgresAuditRecorder(new PostgresAuditOptions {
    ConnectionString = configuration.GetConnectionString("Audit"),
    EnableAutoMigrations = true
});
```

Migrations run at host startup through `IHostedService` when `EnableAutoMigrations` is true, not during service registration. The app needs a host such as
`Host.CreateDefaultBuilder()` or `WebApplication.CreateBuilder()`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)