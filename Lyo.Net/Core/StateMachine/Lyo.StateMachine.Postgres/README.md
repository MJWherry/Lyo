# Lyo.StateMachine.Postgres

PostgreSQL + EF Core implementation of `IStateStore<TState>`, `IStateInstanceQuery<TState>`, and `IStateMachineDefinitionStore` from [
`Lyo.StateMachine.Models`](../Lyo.StateMachine.Models/README.md). Use it with `StateMachineInstance` and `StateMachineDefinitionCache` from [
`Lyo.StateMachine`](../Lyo.StateMachine/README.md) for jobs, workers, and other entities whose state must survive restarts, and for machines defined as data.

## Features

- **Optimistic concurrency.** The version check, instance write, and history row commit in one transaction. A stale writer gets `false` and nothing is written.
- **Data and timers.** Instance data is stored as `jsonb`; the timer due time is indexed with the machine name so a scheduler can poll `ListAsync(new() { DueBeforeUtc = now })`
  cheaply.
- **Queries.** List by states, due time, or update time with paging; count by state; page history oldest first; delete an instance and its history with an optional version check.
- **Transition history.** Each save with a `TransitionRecord` adds a row, unique per (machine, instance, version).
- **Definition documents.** `PostgresStateMachineDefinitionStore` saves each `StateMachineDocument` revision as an immutable `jsonb` row; a publish only succeeds when
  `expectedRevision` is still the latest.
- **Any state type.** Enum states are stored by member name. Supply an `IStateKeyConverter<TState>` for other types.
- **UTC only.** Snapshot, transition, and filter times must be UTC (`ArgumentException` otherwise).

## Examples

### Registration

```csharp
services.AddStateMachinePostgresFromConfiguration(configuration);
services.AddPostgresStateStore<JobState>(); // IStateStore<JobState> + IStateInstanceQuery<JobState>
services.AddPostgresStateMachineDefinitionStore(); // IStateMachineDefinitionStore

// Custom state type
services.AddPostgresStateStore(new MyStateKeyConverter());
```

### Polling due timers

```csharp
var due = await query.ListAsync("job", new() { DueBeforeUtc = DateTime.UtcNow }, take: 50, ct: ct);
foreach (var item in due)
    await new StateMachineInstance<JobState, JobTrigger, JobContext>(definition, store, item.InstanceKey)
        .FireDueAsync(ctx, DateTime.UtcNow, ct);
```

## Database schema

PostgreSQL schema `state_machine`. Table `instance`: primary key (`machine_name`, `instance_key`), `state`, `version` (concurrency token), `entered_timestamp`, `updated_timestamp`,
`due_at`, `data_json` (`jsonb`); indexes on (`machine_name`, `state`) and (`machine_name`, `due_at`). Table `transition`: `id`, `machine_name`, `instance_key`, `from_state`,
`to_state`, `trigger`, `version`, `occurred_timestamp`. Table `definition`: primary key (`name`, `revision`), `document` (`jsonb`), `created_timestamp`. Migrations history lives in
`__EFMigrationsHistory` in the same schema.

## Migrations

At design time set `STATE_MACHINE_CONNECTION_STRING` and run
`dotnet ef migrations add MigrationName --project Core/StateMachine/Lyo.StateMachine.Postgres --context StateMachineDbContext`. Set `EnableAutoMigrations` to apply them at host
start.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.StateMachine` (direct, lyo)
- `Lyo.StateMachine.Models` (direct, lyo)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)