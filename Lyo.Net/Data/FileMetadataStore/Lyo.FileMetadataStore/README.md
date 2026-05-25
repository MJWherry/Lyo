# Lyo.FileMetadataStore

**File identity without bytes.** Large systems split:

1. **Blob storage** (**`Lyo.FileStorage`**) — throughput, multipart uploads, scanners, CDN URLs.
2. **Metadata OLTP** (**this abstraction**) — dedupe fingerprints, encryption key ids, multipart session pointers, archival flags.

Clients depend on **`IFileMetadataStore`** only where they manipulate **canonical `Guid`** file identifiers.

## Methods

| Operation                                                          | Responsibility                                                                                                                                                  |
|--------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`GetMetadataAsync(Guid fileId)`**                                | Hydrates **`FileStoreResult`**; **`FileNotFoundException`** when row missing or logically deleted (**soft delete** hides tombstones here).                                                    |
| **`SaveMetadataAsync(Guid, FileStoreResult)`**                     | Insert or overwrite row keyed by **`fileId`**. Implementations enforce uniqueness on hash/external keys (`PostgresFileMetadataStore` maps fields into columns).                                 |
| **`DeleteMetadataAsync(Guid)`**                                    | **Soft-delete** (sets **`DeletedAt`**, row retained): **`false`** if missing or already deleted; **`GetMetadataAsync`** / **`FindByHashAsync`** omit tombstones.                               |
| **`FindByHashAsync(byte[] hash)`**                                 | Duplicate detection shortcut — ignores soft-deleted rows (often combined with **`Lyo.Hashing`**).                                                                                             |
| **`FindByKeyIdAndVersionAsync(string keyId, string? keyVersion)`** | Key rotation audits — active (non–soft-deleted) metadata only, referencing a KMS/KEK logical key/version pair.                                                                                 |

`FileStoreResult` exposes optional **`DeletedAt`** (UTC) when present in storage; callers treat metadata without it as active.

## Architectural guidance

Treat metadata writes as **eventually consistent** relative to blob existence unless you orchestrate Saga-style compensations (`Save blob → Save metadata`; if latter fails delete
blob).

For multi-tenant systems, prepend tenant key to logical file ids **outside** interface or augment models with partitioning columns inside concrete stores.

Concrete implementations:

- [`FileMetadataStore.Postgres`](../Lyo.FileMetadataStore.Postgres/README.md) (**production OLTP schema** — includes optional audit/multipart adjunct stores).
- [`FileMetadataStore.Sqlite`](../Lyo.FileMetadataStore.Sqlite/README.md) (**placeholder**, no SQLite code yet).

## See also

- [`Lyo.FileStorage`](../../FileStorage/Lyo.FileStorage/README.md) consumes metadata for duplication + encryption bridging.
