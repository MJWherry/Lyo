# Lyo.Audit

Audit trail library with two distinct concepts: **AuditChange** (entity change tracking) and **AuditEvent** (events to log). `AuditChange` and `AuditEvent` are immutable
records—once created they do not change. Includes `IAuditRecorder` for pluggable storage.

## Features

- **AuditChange** (record) – Entity/property change: `Id` (Guid), `Timestamp`, `TypeAssemblyFullName`, `OldValues` (property → old value), `ChangedProperties` (property → new
  value)
- **AuditEvent** (record) – An event to log: `Id` (Guid), `EventType`, `Timestamp`, optional `Message`, `Actor`, and `Metadata`
- **IAuditRecorder** – Interface with sync and async methods: `RecordChange`/`RecordChangeAsync`, `RecordChanges`/`RecordChangesAsync`, `RecordEvent`/`RecordEventAsync`,
  `RecordEvents`/`RecordEventsAsync` (implement to persist to database, log sink, etc.)
- **NullAuditRecorder** – No-op implementation when auditing is not needed

## Quick Start

### AuditChange (entity changes)

```csharp
using Lyo.Audit;

var change = new AuditChange(
    typeof(Order).AssemblyQualifiedName ?? "MyApp.Models.Order, MyApp",
    new Dictionary<string, object?> {
        ["Name"] = "Old Name",
        ["Status"] = "Draft"
    },
    new Dictionary<string, object?> {
        ["Name"] = "New Name",
        ["Status"] = "Submitted"
    });

auditRecorder.RecordChange(change);
```

### AuditEvent (events to log)

```csharp
using Lyo.Audit;

var evt = new AuditEvent(
    "UserLogin",
    "User signed in successfully",
    "user-123",
    new Dictionary<string, object?> {
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

## PostgreSQL persistence

Use **Lyo.Audit.Postgres** for PostgreSQL storage with EF Core migrations:

```xml
<PackageReference Include="Lyo.Audit.Postgres" Version="1.0.22" />
```

```csharp
services.AddPostgresAuditRecorder(new PostgresAuditOptions {
    ConnectionString = configuration.GetConnectionString("Audit"),
    EnableAutoMigrations = true
});
```

When `EnableAutoMigrations` is true, migrations run at **host startup** (via `IHostedService`), not during service registration. Ensure your app uses a host (e.g.
`Host.CreateDefaultBuilder()` or `WebApplication.CreateBuilder()`).

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Audit.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

*None declared in this project file.*

### Project references

- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*3*). Nested types and file-scoped namespaces may omit some entries.

- `IAuditRecorder`
- `IsExternalInit`
- `NullAuditRecorder`

<!-- LYO_README_SYNC:END -->

