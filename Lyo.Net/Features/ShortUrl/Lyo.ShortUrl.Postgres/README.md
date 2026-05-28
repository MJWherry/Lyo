# Lyo.ShortUrl.Postgres

EF Core schema and DbContext registration for a PostgreSQL-backed short-URL store.

> **Heads up — there is no `PostgresShortUrlService` / `PostgresShortUrlStore` in this package.** It deliberately ships only the database surface (DbContext, entities,
> migrations, options, and DI helpers for the context). To get an `IShortUrlService` that talks to this schema, plug your own implementation on top of `ShortUrlDbContext` (or
> use the in-box `Lyo.ShortUrl.ShortUrlService` for id-only generation; it does **not** persist anything).

## What ships

- `ShortUrlDbContext` + `ShortUrlDbContextFactory` under `Database/`, with `DbSet<ShortUrlEntity>` and `DbSet<UrlClickEntity>`. The default schema is `"url"`.
- `ShortUrlEntity` — the canonical short-link row: string-keyed `Id` (max 100 chars) used as the short code, `LongUrl` (≤ 1024), optional `CustomAlias`,
  `CreatedTimestamp` / `UpdatedTimestamp` / `ExpirationDate` / `LastAccessedDate`, `ClickCount`, `IsActive`, and a `Clicks` navigation. Includes a `FromShortenRequest(...)`
  factory and a `BuildShortUrl(baseUrl)` helper that mirrors `Lyo.ShortUrl.ShortUrlService`’s `BaseUrl` behaviour.
- `UrlClickEntity` — append-only click log row (`ShortUrlId` FK, `ClickedAt`, optional `IpAddress` / `UserAgent` / `Referrer`).
- `*EntityConfiguration` Fluent API mappings under the same folder.
- `PostgresShortUrlOptions` (`SectionName = "PostgresShortUrl"`, `Schema = "url"`, `ConnectionString`, `EnableAutoMigrations`).
- `Constants.Metrics` names (`urlshortener.postgres.statistics.duration`, `.delete.duration`, `.update.duration`) — reserved for downstream service implementations.
- `InitialCreate` migration under `Migrations/`.

## DI registration (`Extensions`)

| Entry point                                                                                                           | What it does                                                                                                                                                                                                                |
|-----------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddShortUrlDbContext(string connectionString)`                                                                       | Registers the DbContext factory plus a scoped `ShortUrlDbContext` resolved from the factory.                                                                                                                                |
| `AddShortUrlDbContext(Action<DbContextOptionsBuilder>)`                                                               | Bare `AddDbContext<ShortUrlDbContext>(...)` — bring your own provider configuration.                                                                                                                                        |
| `AddShortUrlDbContextFactory(Action<PostgresShortUrlOptions>)` *(plus `(options)` and `FromConfiguration` overloads)* | Registers `IOptions<PostgresShortUrlOptions>`, `AddPostgresMigrations<ShortUrlDbContext, PostgresShortUrlOptions>()`, and `IDbContextFactory<ShortUrlDbContext>` (`UseNpgsql` + migrations history under the `url` schema). |

There is intentionally no `AddPostgresShortUrlStore` / `AddPostgresShortUrlService` — see the note at the top of this README. Wire your own service on top of the factory, for
example:

```csharp
services.AddShortUrlDbContextFactoryFromConfiguration(configuration);

services.AddSingleton<IShortUrlService, MyEfBackedShortUrlService>();
// where MyEfBackedShortUrlService takes IDbContextFactory<ShortUrlDbContext>
// and IShortUrlGenerator (services.AddShortUrlGenerator()).
```

## Schema

| Table            | Notable columns                                                                                                                                                                 |
|------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `url.short_urls` | `Id` (PK, string ≤ 100), `LongUrl` (≤ 1024), `CustomAlias?` (≤ 100), `CreatedTimestamp`, `UpdatedTimestamp?`, `ExpirationDate?`, `LastAccessedDate?`, `ClickCount`, `IsActive`. |
| `url.url_clicks` | `Id` (PK, `long`), `ShortUrlId` (FK → `short_urls.Id`), `ClickedAt`, optional `IpAddress` (≤ 45), `UserAgent` (≤ 500), `Referrer` (≤ 512).                                      |

(Exact table / column names follow the EF defaults plus the configurations under `Database/`. The migrations history is tracked in `__EFMigrationsHistory` under the same
`url` schema.)

## Dependencies

*(Synchronized from `Lyo.ShortUrl.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)
- [`Lyo.ShortUrl`](../Lyo.ShortUrl/README.md)
