# Lyo.FantasyFootball.Api

Reads come from `IFantasyFootballStore`. Refresh and writes call `FantasyFootballClient`, then upsert what ESPN returns. ESPN cookies stay on the host. `AddFantasyFootballApiForWorkbench` registers the store with automatic migrations. `MapFantasyFootballApi` maps group `FantasyFootball`. A production host should require auth: these routes can post chat messages and trades.

## Features

- `POST /{leagueId}/{seasonId}/refresh` loads the league, roster, chats, and trades.
- Chat routes list topics, list messages, post to a topic, and post to an existing direct message.
- Trade routes list, propose, and respond with accept, decline, uphold, or veto.

## Examples

### Embed in a workbench host

```csharp
services.AddFantasyFootballClientFromConfiguration(configuration);
services.AddFantasyFootballApiForWorkbench(connectionString);
app.MapFantasyFootballApi(allowAnonymous: true);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Espn.Fantasy.Football.Client` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FantasyFootball` (direct, lyo)
- `Lyo.FantasyFootball.Postgres` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.RateLimit` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)