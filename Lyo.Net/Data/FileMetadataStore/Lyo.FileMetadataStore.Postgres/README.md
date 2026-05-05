# Lyo.FileMetadataStore.Postgres

OLTP **`IFileMetadataStore`** plus adjunct services used by richer file pipelines:

### `PostgresFileMetadataStore`

Implements transactional CRUD for canonical metadata rows (**`SaveMetadataAsync`** upserts, **`FindByHashAsync`** leverages indexed digest columns—verify migration indices before huge imports, **`FindByKeyIdAndVersion`** powers rotation reporting).

Registers as **scoped** in many hosts so each ASP.NET HTTP request obtains its own **`FileMetadataStoreDbContext`** lifecycle (prevent accidental cross-request state).

### Auxiliary stores

Implementations packaged here also cover:

| Type                                      | Implements                         | Purpose                                                                                             |
|-------------------------------------------|------------------------------------|-----------------------------------------------------------------------------------------------------|
| **`PostgresFileAuditSink`**               | **`IFileAuditEventHandler`**       | Persists immutable audit timelines for uploads/downloads/deletes surfaced by **`Lyo.FileStorage`**. |
| **`PostgresMultipartUploadSessionStore`** | **`IMultipartUploadSessionStore`** | Tracks staged multipart uploads (part manifests, TTL cleanup policies).                             |

### DI panorama (`Extensions` highlights)

Beyond simple `AddPostgresFileMetadataStore` overloads, you can declare **keyed registrations** (**`AddPostgresFileMetadataStoreKeyed`**) to bind multiple logical stores (different connection strings/schemas) concurrently—essential for gateways hosting **per-tenant** metadata silos without duplicating binaries.

Fluent builder (**`PostgresFileMetadataStoreBuilder.ConfigurePostgresFileStore`**) merges configuration sections + programmatic overrides before **`Build()`** finalizes keyed or default mappings.

Separate methods register **only DbContext factories** (`AddFileMetadataStoreDbContextFactory*`) where an API host wants manual context control (unit-of-work wrappers, custom pooling).

Logs via **`ILogger<PostgresFileMetadataStore>`** at Debug/Warning levels for forensic tracing.

### Failure handling

Translates Postgres unique violations (`23505`) into predictable outcomes where possible (duplicate hash insert collisions). Inspect `catch` scopes before mapping to **`409 Conflict`** externally.

### Migrations / schema coupling

Changing column layout requires coordinated releases with **`Lyo.FileStorage`** expectations (serialized JSON blobs evolve carefully—additive fields preferred).

## See also

- [`Lyo.FileMetadataStore`](../Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md)
- [`Lyo.Postgres`](../../Postgres/Lyo.Postgres/README.md)
