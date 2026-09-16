# Lyo.Outbox

IOutbox does not reference a broker. Delivery is at-least-once. Consumers dedupe on the row id. Enqueue does not commit and does not accept a caller-supplied hash.

## Features

- **Enqueue first.** Write the outbox row, and write business rows only when Inserted is true. A duplicate leaves the caller transaction open. A different payload rolls it back.

## Examples

### Enqueue, then business rows

```csharp
await using var tx = await db.Database.BeginTransactionAsync(ct);
var enqueued = await outbox.EnqueueAsync(tx.GetDbTransaction(), message, ct);
if (!enqueued.Inserted) {
    await tx.RollbackAsync(ct);
    return enqueued;
}
await tx.CommitAsync(ct);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Idempotency` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)