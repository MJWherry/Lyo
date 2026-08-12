# Lyo.ChangeTracker

Generic entity change history built around `Lyo.EntityReference.Models.EntityRef`. Record property-level changes for any entity type without coupling the tracker to a specific
aggregate.

## Features

- `ChangeRecord` for entity-scoped history with old values, changed values, optional **actor** (`FromEntity` at API; `from_entity_*` in DB), and optional `ChangeType` / `Message`
- `IChangeTracker` for recording, querying, and deleting change history
- `NullChangeTracker.Instance` (singleton) when change tracking is optional — all writes/queries are no-ops

## Examples

### Quick Start

```csharp
using Lyo.ChangeTracker;
using Lyo.EntityReference.Models;

var orderRef = EntityRef.ForGuid("Order", Guid.Parse("11111111-1111-1111-1111-111111111111"));
var userRef = EntityRef.ForKey("User", "123");

var change = new ChangeRecord(
    orderRef,
    new Dictionary<string, object?> { ["Status"] = "Draft" },
    new Dictionary<string, object?> { ["Status"] = "Submitted" }) {
    FromEntity = userRef,
    ChangeType = "Updated",
    Message = "Order submitted"
};

await changeTracker.RecordChangeAsync(change);
var history = await changeTracker.GetForEntityAsync(orderRef);
```

## `IChangeTracker` surface

| Method                                                                       | Purpose                                                           |
|------------------------------------------------------------------------------|-------------------------------------------------------------------|
| `RecordChange` / `RecordChangeAsync`                                         | Record a single `ChangeRecord`.                                   |
| `RecordChanges` / `RecordChangesAsync`                                       | Record many records in a single batch (skips empty collections).  |
| `GetByIdAsync(Guid id, …)`                                                   | Look up a recorded change by its `ChangeRecord.Id`.               |
| `GetForEntityAsync(EntityRef forEntity, …)`                                  | Returns history for a specific entity, newest first.              |
| `GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, …)` | Returns history scoped by entity type, optionally filtered by id. |
| `DeleteForEntityAsync(EntityRef forEntity, …)`                               | Deletes all history rows recorded against a specific entity.      |

The base abstraction is purely a contract — health/diagnostics are added by adapter packages (see `Lyo.ChangeTracker.Postgres`).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)