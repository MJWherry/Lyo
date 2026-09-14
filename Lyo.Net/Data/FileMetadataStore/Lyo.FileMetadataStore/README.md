# Lyo.FileMetadataStore

File identity without the bytes. Large systems split:

1. **Blob storage** (Lyo.FileStorage). Throughput, multipart uploads, CDN URLs. 2. **Metadata OLTP** (this abstraction). Dedupe fingerprints, encryption key ids, multipart session pointers, archival flags.

Depend on IFileMetadataStore only where you manipulate canonical Guid file identifiers. `ListByPathPrefixAsync` lists non-deleted rows for a PathPrefix (immediate children or descendants) without a schema change.

## Methods

| Operation | Responsibility |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GetMetadataAsync(Guid fileId) | Yields FileStoreResult. Throws FileNotFoundException if the row is missing or logically deleted. Soft delete keeps tombstones out of this path. |
| SaveMetadataAsync(Guid, FileStoreResult) | Insert or overwrite the row keyed by fileId. Implementations require uniqueness on hash and external keys. PostgresFileMetadataStore maps those fields into columns. |
| DeleteMetadataAsync(Guid) | Soft-delete: sets DeletedAt and Availability=Deleted while keeping the row. Returns false if missing or already deleted. GetMetadataAsync and FindByHashAsync skip tombstones. |
| PurgeMetadataAsync(Guid) | Hard-delete the metadata row, or the .meta JSON on the local store. Idempotent. Returns false when no record existed. Lyo.FileStorage.DeleteFileAsync(..., FileDeletionMode.RemoveObjectAndPurgeMetadata) uses this for retention and governance. |
| FindByHashAsync(byte[] hash) | Shortcut for duplicate detection. Soft-deleted rows are ignored. Often paired with Lyo.Hashing. |
| FindByKeyIdAndVersionAsync(string keyId, string? keyVersion) | For key-rotation audits. Only active (not soft-deleted) metadata that references a KMS/KEK logical key/version pair. |

When storage has it, FileStoreResult exposes optional DeletedAt (UTC). Callers treat metadata without that stamp as active. GetMetadataAsync and FindByHashAsync skip tombstones, and Workbench QueryProject listings do the same.

## FileAvailability states

Availability on `FileStoreResult.Availability` (hosts set scan-related states after ingest processing):

| State | Meaning |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Available | The default. Reads, presigned URLs, and direct downloads are allowed. |
| PendingScan | Host-set while ingest processing (for example ContentThreatScan) is pending. Reads throw `FileNotAvailableException` unless `AllowReadQuarantinedForAdmin` is set. |
| PendingDirectUpload | `BeginDirectUploadAsync` has issued a PUT URL; `CompleteDirectUploadAsync` has not finalized yet. |
| Quarantined | Host-set when content should not be generally readable. Storage policy may still allow admin-only reads. |
| Rejected | Host-set or policy hard-rejected the content. Reads always fail. |
| Deleted | Logical tombstone (`DeletedAt` is set). Reads fail and listings omit the row. Soft-delete sets this rather than leaving `Available`. |

## DekMigrationResult

What Lyo.FileStorage MigrateDeksAsync / RotateDeksAsync returns. Per-file outcomes include Updated, Skipped, and Failed counts plus the failure list, so operators can re-run a key rotation against only the failing subset.

## Local file metadata store DI

For single-host scenarios, Lyo.FileMetadataStore.LocalFileMetadataStore is the JSON-backed implementation. Register it through Extensions:

| Extension | Notes |
| ------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------- |
| `services.AddLocalFileMetadataStore(string rootDirectoryPath)` | Direct path. |
| `services.AddLocalFileMetadataStore(Func<IServiceProvider, string> resolveRoot)` | Resolves the path lazily (handy for tests or DI-derived paths). |
| `services.AddLocalFileMetadataStoreFromConfiguration(IConfiguration, sectionName = LocalFileMetadataStoreOptions.SectionName)` | Bind `LocalFileMetadataStoreOptions` (`RootDirectoryPath`). |
| `services.AddLocalFileMetadataStoreKeyed(string keyName, string rootDirectoryPath)` | Keyed registration that exposes both `LocalFileMetadataStore` and `IFileMetadataStore`. |
| `services.AddLocalFileMetadataStoreKeyed(string keyName)` | Yields a `LocalFileMetadataStoreBuilder` for fluent options/section-based wiring (`ConfigureLocalFileStore(...)`, `Build()`). |

## Download access (`DownloadAccess/`)

Time-boxed download links are store-agnostic, so the contracts sit here instead of in each backend:

| Type | Role |
| --- | --- |
| `IFileDownloadAccessService` | Mint and redeem opaque download tokens. Postgres and SQLite packages implement this. |
| `CreateFileDownloadAccessLinkRequest` / `CreateFileDownloadAccessLinkResult` | Link request (file, expiry, max uses, audit context) and the issued token. |
| `ConsumeFileDownloadAccessLinkResult` + `FileDownloadAccessConsumeFailureReason` | Redemption outcome, with a typed reason (expired, exhausted, unknown, ...) instead of a bare `false`. |
| `FileDownloadAccessToken` | Token mechanics shared by every backend: `Create()` (32 secure random bytes plus its hash), `TryHash`, URL-safe `Encode` / `Decode`, and `LockKey(tokenHash)` for the distributed lock that makes single-use redemption race-free. |

Only the hash is persisted; the raw token exists solely in the `Create` result and in the inbound redemption request.

## Architecture

Unless you orchestrate compensations, treat metadata writes as eventually consistent relative to blob existence (Save blob, then Save metadata. If the latter fails, delete the blob).

For multi-tenant systems, prepend the tenant key to logical file ids outside the interface, or add partitioning columns inside concrete stores.

Concrete implementations:

- [`FileMetadataStore.Postgres`](../Lyo.FileMetadataStore.Postgres/README.md) (OLTP schema, including optional audit/multipart/download-access adjunct stores).
- [`FileMetadataStore.Sqlite`](../Lyo.FileMetadataStore.Sqlite/README.md) (embedded / local-dev SQLite, same adjunct services as Postgres).

## See also

- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md) uses metadata for duplication + encryption bridging.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)