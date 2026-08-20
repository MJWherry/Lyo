# Lyo.FileMetadataStore

File identity without bytes. Large systems split:

1. **Blob storage** (Lyo.FileStorage). Throughput, multipart uploads, scanners, CDN URLs. 2. **Metadata OLTP** (this abstraction). Dedupe fingerprints, encryption key ids, multipart session pointers, archival flags.

Clients depend on IFileMetadataStore only where they manipulate canonical Guid file identifiers.

## Methods

| Operation | Responsibility |
| ------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| GetMetadataAsync(Guid fileId) | Returns FileStoreResult. FileNotFoundException when the row is missing or logically deleted. Soft delete hides tombstones here. |
| SaveMetadataAsync(Guid, FileStoreResult) | Insert or overwrite the row keyed by fileId. Implementations enforce uniqueness on hash and external keys. PostgresFileMetadataStore maps fields into columns. |
| DeleteMetadataAsync(Guid) | Soft-delete: sets DeletedAt, row retained. Returns false if missing or already deleted. GetMetadataAsync and FindByHashAsync omit tombstones. |
| PurgeMetadataAsync(Guid) | Hard-delete the metadata row, or the .meta JSON for the local store. Idempotent. Returns false when there was no record. Used by Lyo.FileStorage.DeleteFileAsync(..., FileDeletionMode.RemoveObjectAndPurgeMetadata) for retention and governance. |
| FindByHashAsync(byte[] hash) | Duplicate detection shortcut. Ignores soft-deleted rows. Often combined with Lyo.Hashing. |
| FindByKeyIdAndVersionAsync(string keyId, string? keyVersion) | Key rotation audits. Active (not soft-deleted) metadata only, referencing a KMS/KEK logical key/version pair. |

FileStoreResult exposes optional DeletedAt (UTC) when present in storage. Callers treat metadata without it as active. GetMetadataAsync and FindByHashAsync omit tombstones. Admin grids and workbench QueryProject views may include soft-deleted rows and should gate mutating actions on DeletedAt.

## FileAvailability states

`FileStoreResult.Availability` propagates content gating decisions from the storage pipeline:

| State | Meaning |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| Available | Default; reads / presigned URLs / direct downloads are permitted. |
| PendingScan | Saved but awaiting a malware/policy scan; `FileNotAvailableException` on read unless `AllowReadQuarantinedForAdmin` is set. |
| PendingDirectUpload | Direct-upload `BeginDirectUploadAsync` has issued a PUT URL but `CompleteDirectUploadAsync` has not finalized. |
| Quarantined | A scan flagged the content. Admin-only read may be allowed by storage policy. |
| Rejected | A scan or policy hard-rejected the content; reads always fail. |

## DekMigrationResult

Returned by MigrateDeksAsync / RotateDeksAsync in Lyo.FileStorage. Records per-file outcomes including Updated, Skipped, and Failed counts plus the failure list, so operators can re-run a key rotation against only the failing subset.

## Local file metadata store DI

Lyo.FileMetadataStore.LocalFileMetadataStore is a JSON-backed implementation for single-host scenarios. Registered through Extensions:

| Extension | Notes |
| ------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| `services.AddLocalFileMetadataStore(string rootDirectoryPath)` | Direct path. |
| `services.AddLocalFileMetadataStore(Func<IServiceProvider, string> resolveRoot)` | Lazy path resolution (useful for tests or DI-derived paths). |
| `services.AddLocalFileMetadataStoreFromConfiguration(IConfiguration, sectionName = LocalFileMetadataStoreOptions.SectionName)` | Bind `LocalFileMetadataStoreOptions` (`RootDirectoryPath`). |
| `services.AddLocalFileMetadataStoreKeyed(string keyName, string rootDirectoryPath)` | Keyed registration with both `LocalFileMetadataStore` and `IFileMetadataStore` exposed. |
| `services.AddLocalFileMetadataStoreKeyed(string keyName)` | Returns a `LocalFileMetadataStoreBuilder` for fluent options/section-based wiring (`ConfigureLocalFileStore(...)`, `Build()`). |

## Architecture

Treat metadata writes as eventually consistent relative to blob existence unless you orchestrate compensations (Save blob, then Save metadata. If the latter fails, delete the blob).

For multi-tenant systems, prepend the tenant key to logical file ids outside the interface, or add partitioning columns inside concrete stores.

Concrete implementations:

- [`FileMetadataStore.Postgres`](../Lyo.FileMetadataStore.Postgres/README.md) (OLTP schema, including optional audit/multipart/staged-upload adjunct stores).
- [`FileMetadataStore.Sqlite`](../Lyo.FileMetadataStore.Sqlite/README.md) (embedded / local-dev SQLite, same adjunct services as Postgres, including `staged_file_upload`).

## See also

- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md) consumes metadata for duplication + encryption bridging.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
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