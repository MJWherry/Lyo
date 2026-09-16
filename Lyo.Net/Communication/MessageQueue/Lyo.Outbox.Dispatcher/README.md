# Lyo.Outbox.Dispatcher

AddOutboxDispatcher claims due rows, commits the lease, then calls SendToQueue. It does not reference a RabbitMQ package. A crash after a successful send and before complete publishes again after the lease expires. Consumers dedupe on the row id.

## Features

- **The lease is committed before publish.** A second worker cannot claim the row until lease_until. A stale worker whose lease was stolen does not clear the new token.
- **Max attempts marks the row failed.** ReplayFailedAsync returns that outbox row to in progress. It does not change API rows.

## Examples

### Register

```csharp
services.AddOutboxDispatcher(new OutboxDispatcherOptions {
    Lease = TimeSpan.FromSeconds(30),
    MaxAttempts = 5
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.MessageQueue` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Outbox.Postgres` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Idempotency` (transitive, lyo)
- `Lyo.Idempotency.Postgres` (transitive, lyo)
- `Lyo.Outbox` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql` `10.0.3` (transitive, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)