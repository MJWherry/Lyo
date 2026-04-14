# Lyo.ChangeTracker

Generic entity change history built around `Lyo.Common.EntityRef`. Record property-level changes for any entity type without coupling the tracker to a specific aggregate.

## Features

- `ChangeRecord` for entity-scoped history with old values, changed values, optional actor, and optional change type/message
- `IChangeTracker` for recording and querying change history
- `NullChangeTracker` when change tracking is optional

## Quick Start

```csharp
using Lyo.ChangeTracker;
using Lyo.Common;

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

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.ChangeTracker.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `System.Text.Json` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Exceptions`

## Public API (generated)

Top-level `public` types in `*.cs` (*2*). Nested types and file-scoped namespaces may omit some entries.

- `IChangeTracker`
- `NullChangeTracker`

<!-- LYO_README_SYNC:END -->

