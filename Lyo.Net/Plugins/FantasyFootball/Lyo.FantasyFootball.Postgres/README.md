# Lyo.FantasyFootball.Postgres

Schema `fantasy_football`. `AddPostgresFantasyFootballStore` and `AddPostgresFantasyFootballStoreFromConfiguration` register an `IDbContextFactory` and `IFantasyFootballStore`. Refresh upserts ESPN ids. A later league save does not delete members or teams that were omitted.

## Features

- `AddPostgresFantasyFootballStore` takes options, an action, or configuration.
- Unique keys are the ESPN league and season, member SWID, team id, topic id, message id, and transaction id.
- Roster replace updates a player still on the period and removes only players that left.

## Examples

### Register the store

```csharp
services.AddPostgresFantasyFootballStoreFromConfiguration(configuration);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FantasyFootball` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Health` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)