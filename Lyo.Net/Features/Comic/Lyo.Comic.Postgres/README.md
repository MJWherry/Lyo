# Lyo.Comic.Postgres

PostgreSQL + EF Core implementation of `Lyo.Comic.IComicStore` (`PostgresComicStore`) via `ComicDbContext`, `PostgresComicOptions`, and `AddPostgresMigrations<ComicDbContext, PostgresComicOptions>` so deployments can apply schema upgrades on startup.

`PostgresComicStore` also implements `Lyo.Health.IHealth` so orchestrators can probe relational connectivity.

## Schema and migrations

Entities under `Database/` cover the domain: `SeriesEntity`, `AlternateTitleEntity`, `VolumeEntity`, `ChapterEntity`, `PageEntity`, and `CharacterEntity`, each with an `*EntityConfiguration` (Fluent API mappings, indexes, and cascading deletes wired so removing a series prunes its volumes/chapters/pages/characters/alternate titles). Migrations live under `Migrations/` (`InitialCreate` is the current baseline) and use the `comic` schema declared on `PostgresComicOptions.Schema`. Never hand-edit migrated columns in production. Ship code-first migrations alongside API changes.

## DI registration (`Extensions`)

All six entry points are exposed as `IServiceCollection` extensions in `Lyo.Comic.Postgres.Extensions`:

| Entry point | What it does |
| ------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddComicDbContextFactory(Action<PostgresComicOptions>)` | Registers `IOptions<PostgresComicOptions>`, `AddPostgresMigrations<ComicDbContext, PostgresComicOptions>()`, and an `IDbContextFactory<ComicDbContext>` (`UseNpgsql` + migrations history under the configured schema). DbContext only, does **not** register `IComicStore`. |
| `AddComicDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresComicOptions.SectionName)` | Same as above; binds from the configuration section (default `"PostgresComic"`). |
| `AddComicDbContextFactory(PostgresComicOptions options)` | Same as above with a pre-built options instance. |
| `AddPostgresComicStore(Action<PostgresComicOptions>)` | Calls `AddComicDbContextFactory(...)` then registers `IComicStore` → singleton `PostgresComicStore`. |
| `AddPostgresComicStoreFromConfiguration(IConfiguration, string sectionName = PostgresComicOptions.SectionName)` | Same as above; binds from configuration. |
| `AddPostgresComicStore(PostgresComicOptions options)` | Same as above with a pre-built options instance, for tests / integration harnesses. |

## `PostgresComicOptions`

| Member | Default | Notes |
| ----------------------- | ----------------- | ---------------------------------------------------------------------------------- |
| `ConnectionString` | empty | Required (validated by all entry points). |
| `EnableAutoMigrations` | `false` | Consumed by `AddPostgresMigrations<>` to gate the hosted startup migration runner. |
| `SectionName` *(const)* | `"PostgresComic"` | Default appsettings section for the `FromConfiguration` overloads. |
| `Schema` *(const)* | `"comic"` | Used for the EF migrations history table and entity configurations. |

## Runtime expectations

`PostgresComicStore` opens a fresh `ComicDbContext` per call via the registered `IDbContextFactory<ComicDbContext>`, so a singleton is safe under concurrent requests. Each `Save*Async` runs `SaveChangesAsync` inside the using-scope of that single context. Callers that need cross-entity atomicity (for example saving a series together with its chapters) should orchestrate transactions above the store. When hosting under `Lyo.Comic.Api` (Lyo-Comic repo): mapper layers translate HTTP DTO ↔ domain records. Enrichment pipelines may batch external metadata. Those pipelines must respect transaction boundaries imposed by callers around `Save*Async` groupings.

## See also

- [`Lyo.Comic`](../Lyo.Comic/README.md). Domain contract reference.
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md). Shared `AddPostgresMigrations<,>` host helper used here.
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md). Health-probe abstraction implemented by `PostgresComicStore`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Comic` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)