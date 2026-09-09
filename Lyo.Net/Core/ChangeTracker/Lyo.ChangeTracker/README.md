# Lyo.ChangeTracker

Generic entity change history around `Lyo.EntityReference.Models.EntityRef`. Record property-level changes for any entity type without tying the tracker to one aggregate.

## Features

- `ChangeRecord` stores entity-scoped history: old values, changed values, optional actor (`FromEntity` at API; `from_entity_*` in DB), and optional `ChangeType` / `Message`
- `IChangeTracker` records, queries, and deletes change history
- `NullChangeTracker.Instance` (singleton) when change tracking is optional. Writes and queries are no-ops.

## Examples

### First steps

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

## `IChangeTracker` methods

| Method | What it does |
| ---------------------------------------------------------------------------- | ----------------------------------------------------------------- |
| `RecordChange` / `RecordChangeAsync` | Write one `ChangeRecord`. |
| `RecordChanges` / `RecordChangesAsync` | Record many records in one batch (empty collections are skipped). |
| `GetByIdAsync(Guid id, …)` | Fetch a stored change by `ChangeRecord.Id`. |
| `GetForEntityAsync(EntityRef forEntity, …)` | History for one entity, newest first. |
| `GetForEntityTypeAsync(string forEntityType, string? forEntityId = null, …)` | History scoped by entity type, optionally filtered by id. |
| `DeleteForEntityAsync(EntityRef forEntity, …)` | Deletes every history row recorded against a given entity. |

Only a contract lives here. Adapter packages supply health and diagnostics. See `Lyo.ChangeTracker.Postgres`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)