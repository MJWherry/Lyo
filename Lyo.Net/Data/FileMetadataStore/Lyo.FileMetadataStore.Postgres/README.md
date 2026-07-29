# Lyo.FileMetadataStore.Postgres

OLTP **`IFileMetadataStore`** plus adjunct services used by richer file pipelines:

### `PostgresFileMetadataStore`

Implements transactional CRUD for canonical metadata rows (**`SaveMetadataAsync`** upserts, **`FindByHashAsync`** leverages indexed digest columns—verify migration indices before
huge imports, **`FindByKeyIdAndVersion`** powers rotation reporting). Rows support logical delete via nullable **`deleted_at`**; **`GetMetadataAsync`**, duplicate hash lookup, and
key-rotation enumeration ignore tombstoned rows. Physical blob lifecycle remains owned by **`Lyo.FileStorage`**.

Registers as **scoped** in many hosts so each ASP.NET HTTP request obtains its own **`FileMetadataStoreDbContext`** lifecycle (prevent accidental cross-request state).

### Auxiliary stores

Implementations packaged here also cover:

| Type | Implements | Purpose |
|-------------------------------------------|------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`PostgresFileAuditSink`** | **`IFileAuditEventHandler`** | Persists immutable audit timelines for uploads/downloads/deletes surfaced by **`Lyo.FileStorage`**. |
| **`PostgresMultipartUploadSessionStore`** | **`IMultipartUploadSessionStore`** | Tracks staged multipart uploads (part manifests, TTL cleanup policies). |
| **`PostgresStagedFileUploadStore`** | **`IStagedFileUploadStore`** | Persists in-flight staged uploads in **`staged_file_upload`** until **commit** into **`file_metadata`**. |
| **`PostgresFileDownloadAccessService`** | **`IFileDownloadAccessService`** | Issues and consumes opaque, time-boxed download tokens — used by hosts that wrap presigned reads with their own access audit (`CreateFileDownloadAccessLinkRequest` / `ConsumeFileDownloadAccessLinkResult`). |

### Health

`PostgresFileMetadataStore` implements **`Lyo.Health.IHealth`** (`HealthCheckName = "PostgresFileMetadataStore"`): a lightweight `SELECT 1`-style probe via the EF Core context that
surfaces connection issues without scanning rows.

### Options — `PostgresFileMetadataStoreOptions`

- `SectionName` (`PostgresFileMetadataStore`) — default appsettings section.
- `Schema` (`filemetadata`) — Postgres schema; `__EFMigrationsHistory` is rooted here.
- `ConnectionString` — required.
- `EnableAutoMigrations` (via `IPostgresMigrationConfig`) — when true, `AddPostgresMigrations<FileMetadataStoreDbContext, PostgresFileMetadataStoreOptions>()` runs migrations at
  host start through **`Lyo.Postgres`**.

### DI panorama (`Extensions` highlights)

| Extension | Purpose |
|-------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddFileMetadataStoreDbContext(connectionString)` / `(Action<DbContextOptionsBuilder>)` | Register the `FileMetadataStoreDbContext` only (no store). |
| `AddFileMetadataStoreDbContextFactory(Action<PostgresFileMetadataStoreOptions>)` / `(PostgresFileMetadataStoreOptions)` | Register only the EF DbContext factory + auto-migrations through **`Lyo.Postgres`**. |
| `AddFileMetadataStoreDbContextFactoryFromConfiguration(configuration, sectionName)` | Same, but bound from a configuration section. |
| `AddPostgresFileMetadataStore()` / `(Action<DbContextOptionsBuilder>)` / `(connectionString)` | Register `PostgresFileMetadataStore` + `IFileMetadataStore` (scoped). |
| `AddPostgresFileMetadataStoreKeyed(keyName, Action<DbContextOptionsBuilder>)` / `(keyName, connectionString)` | Keyed registration; both `PostgresFileMetadataStore` and `IFileMetadataStore` are exposed under `keyName`. |
| `AddPostgresFileMetadataStoreKeyed(keyName)` | Returns a `PostgresFileMetadataStoreBuilder` (`ConfigurePostgresFileStore`, `Build()`) that merges configuration sections + programmatic overrides — essential for gateways hosting per-tenant metadata silos. |
| `AddPostgresFileAuditSink()` | Adds `PostgresFileAuditSink` as scoped `IFileAuditEventHandler`. |
| `AddPostgresMultipartUploadSessionStore()` | Adds the Postgres `IMultipartUploadSessionStore`. Builder usually does this for you when no session store is registered yet. |
| `AddPostgresStagedFileUploadStore()` | Adds `PostgresStagedFileUploadStore` as scoped `IStagedFileUploadStore`. Builder registers this when no staged store is present yet. |
| `AddPostgresFileDownloadAccessService()` | Adds `PostgresFileDownloadAccessService` and `IFileDownloadAccessService`. |

Logs via **`ILogger<PostgresFileMetadataStore>`** at Debug/Warning levels for forensic tracing.

### Failure handling

Translates Postgres unique violations (`23505`) into predictable outcomes where possible (duplicate hash insert collisions). Inspect `catch` scopes before mapping to *
*`409 Conflict`** externally.

### Migrations / schema coupling

Changing column layout requires coordinated releases with **`Lyo.FileStorage`** expectations (serialized JSON blobs evolve carefully—additive fields preferred).

## See also

- [`Lyo.FileMetadataStore`](../Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md)
- [`Lyo.Postgres`](../../Postgres/Lyo.Postgres/README.md)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.FileMetadataStore` — (direct, lyo)
- `Lyo.FileStorage` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Lock` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Compression` — (transitive, lyo)
- `Lyo.ContentThreatScan` — (transitive, lyo)
- `Lyo.Encryption` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.Keystore` — (transitive, lyo)
- `Lyo.Metrics` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `Lyo.Streams` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Buffers` `4.6.0` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)