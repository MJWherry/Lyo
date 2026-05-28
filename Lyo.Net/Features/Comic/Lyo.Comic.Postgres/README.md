# Lyo.Comic.Postgres

PostgreSQL + EF Core implementation of **`Lyo.Comic.IComicStore`** (`PostgresComicStore`) backed by **`ComicDbContext`**, **`PostgresComicOptions`**, and
`AddPostgresMigrations<ComicDbContext, PostgresComicOptions>` so deployments can auto-upgrade schema similarly to other Lyo feature modules.

`PostgresComicStore` also implements **`Lyo.Health.IHealth`** so orchestrators can probe relational connectivity.

## Schema & migrations

Entities under `Database/` cover the full domain: `SeriesEntity`, `AlternateTitleEntity`, `VolumeEntity`, `ChapterEntity`, `PageEntity`, and `CharacterEntity`, each with an
`*EntityConfiguration` (Fluent API mappings, indexes, and cascading deletes wired so removing a series prunes its volumes/chapters/pages/characters/alternate titles).
Migrations live under `Migrations/` (`InitialCreate` is the current baseline) and use the `comic` schema declared on `PostgresComicOptions.Schema`.

Never hand-edit migrated columns in production — ship code-first migrations alongside API changes.

## DI registration (`Extensions`)

All six entry points are exposed as `IServiceCollection` extensions in `Lyo.Comic.Postgres.Extensions`:

| Entry point                                                                                                        | What it does                                                                                                                                                                                                                                                                      |
|--------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddComicDbContextFactory(Action<PostgresComicOptions>)`                                                           | Registers `IOptions<PostgresComicOptions>`, **`AddPostgresMigrations<ComicDbContext, PostgresComicOptions>()`**, and an `IDbContextFactory<ComicDbContext>` (`UseNpgsql` + migrations history under the configured schema). DbContext only — does **not** register `IComicStore`. |
| `AddComicDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresComicOptions.SectionName)` | Same as above; binds from the configuration section (default `"PostgresComic"`).                                                                                                                                                                                                  |
| `AddComicDbContextFactory(PostgresComicOptions options)`                                                           | Same as above with a pre-built options instance.                                                                                                                                                                                                                                  |
| `AddPostgresComicStore(Action<PostgresComicOptions>)`                                                              | Calls `AddComicDbContextFactory(...)` then registers `IComicStore` → singleton `PostgresComicStore`.                                                                                                                                                                              |
| `AddPostgresComicStoreFromConfiguration(IConfiguration, string sectionName = PostgresComicOptions.SectionName)`    | Same as above; binds from configuration.                                                                                                                                                                                                                                          |
| `AddPostgresComicStore(PostgresComicOptions options)`                                                              | Same as above with a pre-built options instance — for tests / integration harnesses.                                                                                                                                                                                              |

### `PostgresComicOptions`

| Member                  | Default           | Notes                                                                              |
|-------------------------|-------------------|------------------------------------------------------------------------------------|
| `ConnectionString`      | empty             | Required (validated by all entry points).                                          |
| `EnableAutoMigrations`  | `false`           | Consumed by `AddPostgresMigrations<>` to gate the hosted startup migration runner. |
| `SectionName` *(const)* | `"PostgresComic"` | Default appsettings section for the `FromConfiguration` overloads.                 |
| `Schema` *(const)*      | `"comic"`         | Used for the EF migrations history table and entity configurations.                |

## Runtime expectations

`PostgresComicStore` opens a fresh `ComicDbContext` per call via the registered `IDbContextFactory<ComicDbContext>`, so it is safe as a singleton under concurrent request load.
Each `Save*Async` runs `SaveChangesAsync` inside the using-scope of that single context — callers that need cross-entity atomicity (for example saving a series together with
its chapters) should orchestrate transactions above the store.

When hosting under [`Lyo.Comic.Api`](../../../Apps/Comic/Lyo.Comic.Api/README.md):

- Mapper layers translate HTTP DTO ↔ domain records.
- Enrichment pipelines may batch external metadata — ensure those respect transaction boundaries imposed by callers around `Save*Async` groupings.

## See also

- [`Lyo.Comic`](../Lyo.Comic/README.md) — domain contract reference.
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) — shared `AddPostgresMigrations<,>` host helper leveraged here.
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md) — health-probe abstraction implemented by `PostgresComicStore`.
