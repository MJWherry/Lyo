# Lyo.Comic.Postgres

PostgreSQL + EF Core implementation of **`Lyo.Comic.IComicStore`** (`PostgresComicStore`) backed by **`ComicDbContext`**, **`PostgresComicOptions`**, and *
*`AddPostgresMigrations<ComicDbContext, PostgresComicOptions>`** so deployments can auto-upgrade schema similarly to other Lyo feature modules.

Implements **`IHealth`** — surface relational connectivity for orchestrators probing worker/API pods.

## Schema & migrations

Migrations reside under **`Migrations/`**. Entities map series/volumes/chapters/pages/characters/alternate titles with foreign keys aligning to store operations (consult
`*_Entity.cs` + configurations for cascades—you want deletes on series to prune volumes/chapters unless you purposely soft-delete in future revisions).

Never hand-edit migrated columns in production—ship code-first migrations alongside API changes.

## DI registration (`Extensions`)

| Entry point                                                  | Meaning                                                                                                                 |
|--------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------|
| **`AddPostgresComicStore(Action<PostgresComicOptions>)`**    | Fluent configure (connection string, auto-migrations flag…). Registers DbContext factory + singleton **`IComicStore`**. |
| **`AddPostgresComicStoreFromConfiguration(IConfiguration)`** | Binds **`PostgresComicOptions`** defaults section.                                                                      |
| **`AddPostgresComicStore(PostgresComicOptions)`**            | Preconstructed options—for tests/integration harnesses.                                                                 |

Internal steps mirror other Postgres features: **`AddSingleton<IOptions<PostgresComicOptions>>`**, **`IDbContextFactory<ComicDbContext>`**, `UseNpgsql` with migrations history
schema from options.

## Runtime expectations

**`PostgresComicStore`** uses factory-per-operation patterns consistent with concurrency-safe deployments (inspect class for tracked entity usage).

When hosting under [`Lyo.Comic.Api`](../../../Apps/Comic/Lyo.Comic.Api/README.md):

- Mapper layers translate HTTP DTO ↔ domain records.
- Enrichment pipelines may batch external metadata—ensure those respect transaction boundaries imposed by callers around **`Save*Async`** groupings.

## See also

- [`Lyo.Comic`](../Lyo.Comic/README.md) — domain contract reference.
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) — shared autop migration hosting helpers leveraged here.
