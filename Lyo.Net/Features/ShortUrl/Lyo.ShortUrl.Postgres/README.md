# Lyo.ShortUrl.Postgres

EF Core schema and DbContext registration for a PostgreSQL-backed short-URL store.

> There is no `PostgresShortUrlService` / `PostgresShortUrlStore` in this package. It ships the database pieces (DbContext, entities, > migrations, options, and DI helpers for the context). To get an `IShortUrlService` that talks to this schema, implement one on `ShortUrlDbContext` (or > use `Lyo.ShortUrl.ShortUrlService` for id-only generation. That class does not persist anything).

## Examples

### DI registration (`Extensions`)

```csharp
services.AddShortUrlDbContextFactoryFromConfiguration(configuration);

services.AddSingleton<IShortUrlService, MyEfBackedShortUrlService>();
// where MyEfBackedShortUrlService takes IDbContextFactory<ShortUrlDbContext>
// and IShortUrlGenerator (services.AddShortUrlGenerator()).
```

## What ships

- `ShortUrlDbContext` + `ShortUrlDbContextFactory` under `Database/`, with `DbSet<ShortUrlEntity>` and `DbSet<UrlClickEntity>`. The default schema is `"url"`.
- `ShortUrlEntity`. The canonical short-link row: string-keyed `Id` (max 100 chars) used as the short code, `LongUrl` (≤ 1024), optional `CustomAlias`, `CreatedTimestamp` / `UpdatedTimestamp` / `ExpirationDate` / `LastAccessedDate`, `ClickCount`, `IsActive`, and a `Clicks` navigation. Includes a `FromShortenRequest(...)` factory and a `BuildShortUrl(baseUrl)` helper that mirrors `Lyo.ShortUrl.ShortUrlService`'s `BaseUrl` behaviour.
- `UrlClickEntity`. Append-only click log row (`ShortUrlId` FK, `ClickedAt`, optional `IpAddress` / `UserAgent` / `Referrer`).
- `*EntityConfiguration` Fluent API mappings under the same folder.
- `PostgresShortUrlOptions` (`SectionName = "PostgresShortUrl"`, `Schema = "url"`, `ConnectionString`, `EnableAutoMigrations`).
- `Constants.Metrics` names (`urlshortener.postgres.statistics.duration`, `.delete.duration`, `.update.duration`), reserved for downstream service implementations.
- `InitialCreate` migration under `Migrations/`.

## DI registration (`Extensions`)

| Entry point | What it does |
| --------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddShortUrlDbContext(string connectionString)` | Registers the DbContext factory plus a scoped `ShortUrlDbContext` resolved from the factory. |
| `AddShortUrlDbContext(Action<DbContextOptionsBuilder>)` | Bare `AddDbContext<ShortUrlDbContext>(...)`. Bring your own provider configuration. |
| `AddShortUrlDbContextFactory(Action<PostgresShortUrlOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Registers `IOptions<PostgresShortUrlOptions>`, `AddPostgresMigrations<ShortUrlDbContext, PostgresShortUrlOptions>()`, and `IDbContextFactory<ShortUrlDbContext>` (`UseNpgsql` + migrations history under the `url` schema). |

There is intentionally no `AddPostgresShortUrlStore` / `AddPostgresShortUrlService`. See the note at the top of this README. Wire your own service on top of the factory, for
example:

## Schema

| Table | Notable columns |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `url.short_urls` | `Id` (PK, string ≤ 100), `LongUrl` (≤ 1024), `CustomAlias?` (≤ 100), `CreatedTimestamp`, `UpdatedTimestamp?`, `ExpirationDate?`, `LastAccessedDate?`, `ClickCount`, `IsActive`. |
| `url.url_clicks` | `Id` (PK, `long`), `ShortUrlId` (FK → `short_urls.Id`), `ClickedAt`, optional `IpAddress` (≤ 45), `UserAgent` (≤ 500), `Referrer` (≤ 512). |

(Exact table / column names follow the EF defaults plus the configurations under `Database/`. The migrations history is tracked in `__EFMigrationsHistory` under the same
`url` schema.)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.ShortUrl` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)