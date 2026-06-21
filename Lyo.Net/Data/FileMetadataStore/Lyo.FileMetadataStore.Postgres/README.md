# Lyo.FileMetadataStore.Postgres

OLTP **`IFileMetadataStore`** plus adjunct services used by richer file pipelines:

### `PostgresFileMetadataStore`

Implements transactional CRUD for canonical metadata rows (**`SaveMetadataAsync`** upserts, **`FindByHashAsync`** leverages indexed digest columns—verify migration indices before
huge imports, **`FindByKeyIdAndVersion`** powers rotation reporting). Rows support logical delete via nullable **`deleted_at`**; **`GetMetadataAsync`**, duplicate hash lookup, and
key-rotation enumeration ignore tombstoned rows. Physical blob lifecycle remains owned by **`Lyo.FileStorage`**.

Registers as **scoped** in many hosts so each ASP.NET HTTP request obtains its own **`FileMetadataStoreDbContext`** lifecycle (prevent accidental cross-request state).

### Auxiliary stores

Implementations packaged here also cover:

| Type                                      | Implements                         | Purpose                                                                                                                                                                                                       |
|-------------------------------------------|------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`PostgresFileAuditSink`**               | **`IFileAuditEventHandler`**       | Persists immutable audit timelines for uploads/downloads/deletes surfaced by **`Lyo.FileStorage`**.                                                                                                           |
| **`PostgresMultipartUploadSessionStore`** | **`IMultipartUploadSessionStore`** | Tracks staged multipart uploads (part manifests, TTL cleanup policies).                                                                                                                                       |
| **`PostgresStagedFileUploadStore`**       | **`IStagedFileUploadStore`**       | Persists in-flight staged uploads in **`staged_file_upload`** until **commit** into **`file_metadata`**.                                                                                                      |
| **`PostgresFileDownloadAccessService`**   | **`IFileDownloadAccessService`**   | Issues and consumes opaque, time-boxed download tokens — used by hosts that wrap presigned reads with their own access audit (`CreateFileDownloadAccessLinkRequest` / `ConsumeFileDownloadAccessLinkResult`). |

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

| Extension                                                                                                               | Purpose                                                                                                                                                                                                        |
|-------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddFileMetadataStoreDbContext(connectionString)` / `(Action<DbContextOptionsBuilder>)`                                 | Register the `FileMetadataStoreDbContext` only (no store).                                                                                                                                                     |
| `AddFileMetadataStoreDbContextFactory(Action<PostgresFileMetadataStoreOptions>)` / `(PostgresFileMetadataStoreOptions)` | Register only the EF DbContext factory + auto-migrations through **`Lyo.Postgres`**.                                                                                                                           |
| `AddFileMetadataStoreDbContextFactoryFromConfiguration(configuration, sectionName)`                                     | Same, but bound from a configuration section.                                                                                                                                                                  |
| `AddPostgresFileMetadataStore()` / `(Action<DbContextOptionsBuilder>)` / `(connectionString)`                           | Register `PostgresFileMetadataStore` + `IFileMetadataStore` (scoped).                                                                                                                                          |
| `AddPostgresFileMetadataStoreKeyed(keyName, Action<DbContextOptionsBuilder>)` / `(keyName, connectionString)`           | Keyed registration; both `PostgresFileMetadataStore` and `IFileMetadataStore` are exposed under `keyName`.                                                                                                     |
| `AddPostgresFileMetadataStoreKeyed(keyName)`                                                                            | Returns a `PostgresFileMetadataStoreBuilder` (`ConfigurePostgresFileStore`, `Build()`) that merges configuration sections + programmatic overrides — essential for gateways hosting per-tenant metadata silos. |
| `AddPostgresFileAuditSink()`                                                                                            | Adds `PostgresFileAuditSink` as scoped `IFileAuditEventHandler`.                                                                                                                                               |
| `AddPostgresMultipartUploadSessionStore()`                                                                              | Adds the Postgres `IMultipartUploadSessionStore`. Builder usually does this for you when no session store is registered yet.                                                                                   |
| `AddPostgresStagedFileUploadStore()`                                                                                    | Adds `PostgresStagedFileUploadStore` as scoped `IStagedFileUploadStore`. Builder registers this when no staged store is present yet.                                                                         |
| `AddPostgresFileDownloadAccessService()`                                                                                | Adds `PostgresFileDownloadAccessService` and `IFileDownloadAccessService`.                                                                                                                                     |

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
