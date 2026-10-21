# Lyo.Idempotency

A scope and key claim one in-progress row. API rows do not steal: a crashed in-progress row stays in progress until expires_at or Abandon. The same hash replays a completed or
failed response. A different hash is a conflict. Scopes that start with outbox/ are reserved for the outbox.

## Features

- **API keys do not steal.** A crashed in-progress API row stays in progress until expires_at (default 24 hours) or Abandon. Stealing would run the work twice.
- **Replay includes failures.** Fail stores the status and body the same way Complete does. A process crash before either call stays in progress.
- **Equal to utc now is expired.** After the sweeper marks the row expired, the key can be used again. The sweeper does not expire outbox rows.

## Examples

### Claim and finish

```csharp
var hash = IdempotencyHash.Of(body);
var begin = await store.TryBeginAsync(scope, key, hash, ct: ct);
if (begin.Outcome != IdempotencyOutcome.Began)
    return begin;
await store.CompleteAsync(begin.Id, begin.LeaseToken!.Value, 200, body, "application/json", ct);
```

## Recovery

API rows use a lease that lasts until expires_at. Complete and Fail require that lease token. A wrong token does not change the row. The in-memory store throws
NotSupportedException when a DbTransaction is passed. The Postgres store joins a caller transaction with a savepoint so a unique violation does not abort the caller transaction.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)