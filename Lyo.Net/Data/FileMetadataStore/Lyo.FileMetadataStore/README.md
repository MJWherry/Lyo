# Lyo.FileMetadataStore

**File identity without bytes.** Large systems split:

1. **Blob storage** (**`Lyo.FileStorage`**) — throughput, multipart uploads, scanners, CDN URLs.
2. **Metadata OLTP** (**this abstraction**) — dedupe fingerprints, encryption key ids, multipart session pointers, archival flags.

Clients depend on **`IFileMetadataStore`** only where they manipulate **canonical `Guid`** file identifiers.

## Methods

| Operation | Responsibility |
|-----------|----------------|
| **`GetMetadataAsync(Guid fileId)`** | Hydrates **`FileStoreResult`**; **`FileNotFoundException`** when row missing (callers distinguish 404 mappings). |
| **`SaveMetadataAsync(Guid, FileStoreResult)`** | Insert or overwrite row keyed by **`fileId`**. Implementations enforce uniqueness on hash/external keys (`PostgresFileMetadataStore` maps fields into columns). |
| **`DeleteMetadataAsync(Guid)`** | Returns **`false`** if absent (**idempotent deletes** OK). |
| **`FindByHashAsync(byte[] hash)`** | Duplicate detection shortcut (often combined with cryptographic hash algorithms from **`Lyo.Hashing`**). |
| **`FindByKeyIdAndVersionAsync(string keyId, string? keyVersion)`** | Key rotation audits—enumerate blob metadata referencing a KMS/KEK logical key/version pair. |

`FileStoreResult` (under **`Lyo.FileMetadataStore.Models`**) aggregates whatever your blob layer needs persisted (MIME hints, **`FileUploadState`**, encryption headers, multipart ETags). Keep it versioning-tolerant—adding JSON columns in Postgres shouldn’t force API breaks if clients ignore unknown fields.

## Architectural guidance

Treat metadata writes as **eventually consistent** relative to blob existence unless you orchestrate Saga-style compensations (`Save blob → Save metadata`; if latter fails delete blob).

For multi-tenant systems, prepend tenant key to logical file ids **outside** interface or augment models with partitioning columns inside concrete stores.

Concrete implementations:

- [`FileMetadataStore.Postgres`](../Lyo.FileMetadataStore.Postgres/README.md) (**production OLTP schema** — includes optional audit/multipart adjunct stores).
- [`FileMetadataStore.Sqlite`](../Lyo.FileMetadataStore.Sqlite/README.md) (**placeholder**, no SQLite code yet).

## See also

- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md) consumes metadata for duplication + encryption bridging.
