# Lyo.FileStorage

Production-oriented **file storage** for .NET: save / stream-save / read / delete / metadata with optional **compression** ([
`Lyo.Compression`](../../Compression/Lyo.Compression/README.md)), **two-key encryption** ([`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)), **duplicate
hashing**, **access policies**, **malware scans**, **audit hooks**, **multipart uploads** (via [`IMultipartUploadService`](Multipart/IMultipartUploadService.cs)), and
**presigned/direct-upload/copy** on cloud-capable backends.

With XML documentation generation enabled in the repo (**`GenerateDocumentationFile`** in **`Directory.Build.props`**), IntelliSense surfaces the same summaries as this README for
documented members.

## Examples

### Local file storage

```csharp
using Lyo.FileStorage;
using Lyo.FileStorage.Models;

var options = new DiskFileStorageOptions
{
    RootDirectoryPath = "/path/to/storage",
    EnableDuplicateDetection = true,
    DuplicateStrategy = DuplicateHandlingStrategy.ReturnExisting
};

var service = new LocalFileStorageService(options);

// Save a file
var data = File.ReadAllBytes("document.pdf");
var result = await service.SaveFileAsync(
    data,
    originalFileName: "document.pdf",
    compress: true,
    encrypt: true,
    keyId: "my-encryption-key");

// Retrieve a file
var retrievedData = await service.GetFileAsync(result.Id);

// Delete a file
await service.DeleteFileAsync(result.Id);
```

### S3 storage (AWS, Backblaze B2, MinIO, Wasabi, Cloudflare R2, …)

```csharp
using Lyo.FileStorage.S3;
using Lyo.FileStorage.Models;

var options = new S3FileStorageOptions
{
    BucketName = "my-bucket",
    Region = "us-east-1",
    AccessKeyId = "your-access-key",
    SecretAccessKey = "your-secret-key"
};

var metadataStore = new YourMetadataStore(); // IFileMetadataStore
var service = new S3FileStorageService(options, metadataStore);

// Same IFileStorageService contract as LocalFileStorageService
```

## Documentation map

| Document                                                                      | Scope                                                                                                                                                           |
|-------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **This README**                                                               | Core **`IFileStorageService`**, **`LocalFileStorageService`**, options/DTOs, backend capability matrix                                                          |
| [`FileStorageArchitecture.drawio`](FileStorageArchitecture.drawio)            | Multi-page diagram: **overview**, upload (general), **save (compress·encrypt)**, **staged upload**, **read**, **copy**, **DEK migrate**, **DEK rotate**, delete |
| **`Lyo.FileStorage.S3/README.md`**: S3-compatible storage (AWS, B2, MinIO, …) | [`S3FileStorageService.cs`](../Lyo.FileStorage.S3/S3FileStorageService.cs), **`S3FileStorageOptions`**, DI builders                                             |
| **`Lyo.FileStorage.AzureBlob/README.md`**: Azure Blob                         | [`AzureBlobFileStorageService.cs`](../Lyo.FileStorage.AzureBlob/AzureBlobFileStorageService.cs), **`AzureBlobFileStorageOptions`**, SAS / SSE notes             |
| **`Lyo.FileStorage.Web.Components`**: Workbench UI                            | Blazor grids and dialogs that call a configured Test API                                                                                                        |

For multipart session stores and Postgres metadata, follow references from your host registration (e.g. **`Lyo.FileMetadataStore.Postgres`**).

## Backend capability matrix

| Capability                                                                                        | Local disk (**`LocalFileStorageService`**)                                                                                       | S3 (**`S3FileStorageService`**)             | Blob (**`AzureBlobFileStorageService`**)   |
|---------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------|--------------------------------------------|
| **Presigned GET**                                                                                 | Only when **`DiskFileStorageOptions.AllowFileUriPresignedUrls`** (returns **`file://`**, dev-only); no response-header overrides | Yes (incl. **`PreSignedReadUrlOptions`**)   | Yes (SAS + optional response headers)      |
| **Direct PUT upload** (**`BeginDirectUpload` / `CompleteDirectUpload`**)                          | Yes when **`DirectUploadReceiveBaseUri`** is set (PUT URL hits Test API / host receiver); otherwise **`ConfigurationException`** | Yes                                         | Yes                                        |
| **Server-sideCopy** (**`CopyFileAsync`**)                                                         | Yes (filesystem copy + metadata)                                                                                                 | Yes (`CopyObject`)                          | Yes (same API)                             |
| **Move** (**`MoveFileAsync`**)                                                                    | Yes (`File.Move` + metadata; same file id)                                                                                       | Yes (`CopyObject` then delete source)       | Yes (`SyncCopyFromUri` then delete source) |
| **Rename** (**`RenameFileAsync`**)                                                                | Yes (metadata `OriginalFileName` only)                                                                                           | Yes (same)                                  | Yes (same)                                 |
| **Diagnostics listing** (**`IFileStorageDiagnosticsService`**)                                    | Yes (relative paths under **`RootDirectoryPath`**)                                                                               | Yes (combined **`KeyPrefix`**)              | Yes                                        |
| **Multipart** (**`AddLocalMultipartUploadService` / …S3/Blob**)                                   | Yes (server-staged parts)                                                                                                        | Yes                                         | Yes                                        |
| **Staged upload** (**`IStagedFileUploadService` / `AddLocalStagedFileUploadService` / …S3/Blob**) | Yes when **`DirectUploadReceiveBaseUri`** is set (API PUT to `.stage/`); otherwise **`ConfigurationException`**                  | Yes (presigned PUT to `.stage/{id}/object`) | Yes (SAS PUT; not with SSE-C customer key) |

Plaintext direct uploads (**`BeginDirectUpload`**) deliberately **exclude** encryption/compression on the edge PUT; finalized metadata runs through normal
policy/malware/availability flags.

**Staged uploads** use a separate **`staged_file_upload`** table (not **`file_metadata`** until **commit**). **Complete** verifies and hashes the staged object; **commit** runs the
normal compress/encrypt pipeline (API or job worker via **`UploadCompleted`** events).

## Backend capability matrix — **`CancellationToken`** and cloud signing

**`ct`** honours cooperative cancellation during most async I/O. **Presigned URL generation**, some **AWS signing**, and synchronous SDK paths may complete even after cancellation
is requested — see **`GetPreSignedReadUrlAsync`** and related remarks on **`IFileStorageService`**.

## Contracts — **`IFileStorageService`**

- **Writes** — **`SaveFileAsync`**, **`SaveFromStreamAsync`**
- **Reads** — **`GetFileAsync`**, **`GetFileStreamAsync`** (compressed payloads are decompressed via **`ICompressionService.Resolver`** using stored **`CompressionAlgorithm`**
  metadata; optional per-call **`compressionAlgorithmOverride`** or **`FileStorageServiceBaseOptions.DecompressionAlgorithmOverride`**; bounded by **`MaxDecompressedFileSize`**
  through **`MaxBytesWriteStream`**)
- **Metadata / delete** — **`GetMetadataAsync`**, **`DeleteFileAsync(Guid, FileDeletionMode, CancellationToken)`** (deletes the backing object then, depending on * *
  `FileDeletionMode`**, either *tombstones* metadata (`RemoveObjectAndTombstoneMetadata`, default) or *purges* it via **`IFileMetadataStore.PurgeMetadataAsync`**
  ( `RemoveObjectAndPurgeMetadata` — operator/governance flows only; never accept this mode from end-user input)
- **Temporary links** — **`GetPreSignedReadUrlAsync`** (with optional **`PreSignedReadUrlOptions`** overrides; both overloads accept **`CancellationToken`** with a default of
  `default`)
- **Direct single PUT** — **`BeginDirectUploadAsync`**, **`CompleteDirectUploadAsync`** (S3/Blob populate **`RequiredPutHeaders`** for SSE + signed `Content-Type` parity)
- **Copy** — **`CopyFileAsync`** (new file id)
- **Move / rename** — **`MoveFileAsync`** (same file id, relocate by **`PathPrefix`**); **`RenameFileAsync`** (metadata **`OriginalFileName`** only)
- **Key ops** — **`MigrateDeksAsync`**, **`RotateDeksAsync`** (results split **`Skipped`** vs **`Failed`**; missing blobs and short encryption headers fail-fast)
- **Events** — **`FileSaved`**, **`FileRetrieved`**, **`FileDeleted`**, **`FileMoved`**, **`FileRenamed`**, **`FileMetadataRetrieved`**, **`FileAuditOccurred`**. Payloads carry a
  redacted **`FileStoreSnapshot`** (wrapped DEK and KEK salt omitted)
- **Health** — **`CheckHealthAsync`** via **`IHealth`** (lightweight vs full via **`HealthCheckMode`**)

## Contracts — **`IFileStorageDiagnosticsService`**

Optional **`ListStorageKeysAsync`** implemented by backends that can enumerate object/path keys (Local, S3, Blob). All implementations normalize the caller-supplied prefix through
**`Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix`** — the same helper that backs **`FileStorageServiceBase.ValidatePathPrefix`** — which strips wrapping slashes and
rejects `..` segments, doubled separators, and embedded `\0` with **`ArgumentException`** (HTTP 400 in ASP.NET).

## Contracts — **`IMultipartUploadService`**

Large-file client-part uploads; register multipart services together with keyed **`IFileStorageService`** (**`Extensions.AddLocalMultipartUploadService`** or S3/Blob equivalents).

## Contracts — **`IStagedFileUploadService`**

Two-phase uploads for large or untrusted client payloads: **begin** → client PUT to a staging key (`…/.stage/{stageId}/object`) → **complete** (verify/hash) → **commit**
(compress/encrypt into canonical storage). Session state lives in **`IStagedFileUploadStore`** / **`staged_file_upload`** — not **`file_metadata`** until commit.

| Step | Method              | Notes                                                                                                                           |
|------|---------------------|---------------------------------------------------------------------------------------------------------------------------------|
| 1    | **`BeginAsync`**    | Returns presigned/SAS PUT URL + **`RequiredPutHeaders`**.                                                                       |
| 2    | Client PUT          | S3/Blob: direct to cloud URL. Local: **`PUT …/stage/{stageId}/put`** via Test API when **`DirectUploadReceiveBaseUri`** is set. |
| 3    | **`CompleteAsync`** | Confirms object exists, hashes bytes, sets status **`Uploaded`**.                                                               |
| 4    | **`CommitAsync`**   | Runs normal save pipeline; optional **`StagedUploadCommitRequest.Compress`** / **`Encrypt`**.                                   |
| —    | **`AbortAsync`**    | Deletes staging object best-effort.                                                                                             |
| —    | **`GetAsync`**      | Current stage snapshot.                                                                                                         |

Register **`AddLocalStagedFileUploadService`**, **`AddKeyedS3StagedFileUploadService`**, or **`AddAzureBlobStagedFileUploadService`**. Postgres/Sqlite metadata builders
auto-register *
*`PostgresStagedFileUploadStore`** / **`SqliteStagedFileUploadStore`** when no store is present. Hook **`IStagedFileUploadEventHandler`** (or service events) to enqueue async
commit workers after **`UploadCompleted`**.

## Service matrix (**`Lyo.FileStorage`** assembly)

| Type                                               | Role                                                                                                                  |
|----------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------|
| **`LocalFileStorageService`**                      | **`DiskFileStorageOptions`**, **`IFileMetadataStore`**, path sharding under root                                      |
| **`FileStorageServiceBase`**                       | Shared pipeline for save/read/delete, hashing, auditing, multipart plumbing                                           |
| **`Extensions`** — namespace **`Lyo.FileStorage`** | Keyed **`IFileStorageService`** registration for disk, multipart session helpers, **`IFileOperationContextAccessor`** |

## Service matrix (**`Lyo.FileStorage`** assembly) — Extension points

| Type                                                                                                                                 | Registered via                                                                                                                                                                                            | Purpose                                                                                                                                                                  |
|--------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`IFileOperationContextAccessor`**                                                                                                  | **`services.AddFileOperationContextAccessor()`**                                                                                                                                                          | Async-local tenant / actor / correlation propagation surfaced on **`FileAuditEventArgs`** and policy callbacks. The default implementation is registered as a singleton. |
| **`IFileAuditEventHandler`**                                                                                                         | Register as many as you want (`services.AddScoped<IFileAuditEventHandler, MySink>()`). **`AddPostgresFileAuditSink()`** in **`Lyo.FileMetadataStore.Postgres`** wires the bundled Postgres sink.          | Fan-out audit handlers invoked for save / read / delete / metadata / policy events.                                                                                      |
| **`IFileContentPolicy`**                                                                                                             | Register a single implementation (`services.AddScoped<IFileContentPolicy, MyPolicy>()`).                                                                                                                  | Optional pre-save gate that can reject by content (extension/MIME/header sniffing); rejections raise **`FilePolicyRejectedException`**.                                  |
| **`IFileMalwareScanner`** / **`CompositeFileMalwareScanner`** / **`ContentThreatMalwareScanner`** (from **`Lyo.ContentThreatScan`**) | Register the chosen scanner; **`CompositeFileMalwareScanner`** composes multiple.                                                                                                                         | Per-byte scan integration. `RequireScanBeforeAvailable` + missing scanner = fail-closed across save / stream / direct-upload paths.                                      |
| **`IMultipartUploadSessionStore`**                                                                                                   | **`AddInMemoryMultipartUploadSessionStore()`** / **`TryAddInMemoryMultipartUploadSessionStoreIfMissing()`** in-process default; **`AddPostgresMultipartUploadSessionStore()`** for Postgres.              | Tracks staged multipart upload sessions; required by `Local`/`S3`/`Blob` multipart services.                                                                             |
| **`IStagedFileUploadStore`**                                                                                                         | **`AddInMemoryStagedFileUploadStore()`** / **`TryAddInMemoryStagedFileUploadStoreIfMissing()`**; **`AddPostgresStagedFileUploadStore()`** / **`AddSqliteStagedFileUploadStore()`** via metadata builders. | Persists in-flight staged uploads in **`staged_file_upload`** until **commit**.                                                                                          |
| **`IStagedFileUploadEventHandler`**                                                                                                  | Register implementations (`services.AddScoped<IStagedFileUploadEventHandler, MyPublisher>()`).                                                                                                            | Optional lifecycle fan-out (e.g. RabbitMQ commit worker after **`UploadCompleted`**).                                                                                    |

## Multipart, direct upload, DEK operations

- **`FileStorageDekOperations`** — implements **`MigrateDeksAsync`** and **`RotateDeksAsync`** uniformly across backends, including short-encryption-header detection and per-file
  failure isolation.
- **`FileStorageStreamingPipelines`** — composes compression / encryption / hash / max-size guards over streamed save and read paths.
- **`PlainDirectUploadCoordinator`** — finalizes plaintext direct-upload PUTs into a normal **`SaveFileAsync`** outcome (policy, scan, availability) and runs only when the caller
  used **`BeginDirectUploadAsync`** without compression/encryption hints.
- **`StagedUploadCoordinator`** — shared begin/complete/commit/abort orchestration for **`IStagedFileUploadService`**; backend packages plug in **`IStagedFilePhysicalIO`**
  (presigned PUT, stat, read, delete).

## Options — **`FileStorageServiceBaseOptions`** / **`DiskFileStorageOptions`**

**`DiskFileStorageOptions`** adds:

| Property                                                                                        | Typical use                                                                                                                                                                                                                                                                                                                                                                        |
|-------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`RootDirectoryPath`**                                                                         | Root folder for blobs and bundled JSON metadata (when **`IFileMetadataStore`** not injected explicitly)                                                                                                                                                                                                                                                                            |
| **`EnableMetrics`**                                                                             | Emit metrics via **`IMetrics`** when configured                                                                                                                                                                                                                                                                                                                                    |
| **`AllowFileUriPresignedUrls`**                                                                 | (**Dev**) allow **`file://`** presigned-style URLs instead of rejecting presigned reads                                                                                                                                                                                                                                                                                            |
| **`DirectUploadReceiveBaseUri`**                                                                | Absolute origin of the host that exposes **`PUT …/Workbench/FileStorage/direct-upload/{fileId}/put`** (matches **Lyo.TestApi** conventions). When null, **`BeginDirectUploadAsync`** delegates to **`NotSupported`**                                                                                                                                                               |
| **`DirectUploadPutRouteRelativePath`**                                                          | Path between base URI and **`{fileId}/put`**; default **`Workbench/FileStorage/direct-upload`** with the bundled Test API                                                                                                                                                                                                                                                          |
| **Inherited (`FileStorageServiceBaseOptions`)**                                                 |                                                                                                                                                                                                                                                                                                                                                                                    |
| **`HealthCheckMode`**                                                                           | Lightweight vs fuller health probes                                                                                                                                                                                                                                                                                                                                                |
| **`HashAlgorithm`**, **`EnableDuplicateDetection`**, **`DuplicateStrategy`**                    | Dedup by plaintext `originalFileHash`; see [Duplicate detection](#duplicate-detection)                                                                                                                                                                                                                                                                                             |
| **`ThrowOnFileNotFound`**, **`ThrowOnDeleteNotFound`**, **`ThrowOnHashMismatch`**               | Failure-vs-null behaviour                                                                                                                                                                                                                                                                                                                                                          |
| **`MaxUploadSizeBytes`**, **`MaxDecompressedFileSize`**, **`AllowedContentTypes`**              | Safety / validation. `MaxUploadSizeBytes` is enforced on direct-upload PUT bodies in addition to streamed saves; an **empty** `AllowedContentTypes` list **denies** all uploads (configure null or omit to allow any).                                                                                                                                                             |
| **`RequireScanBeforeAvailable`**, **`DefaultAvailability`**, **`AllowReadQuarantinedForAdmin`** | Availability + **`IFileMalwareScanner`** integration. When `RequireScanBeforeAvailable` is true and no scanner is registered, saves **fail-closed** across `byte[]`/stream/direct-upload paths. The chained **`CompositeFileMalwareScanner`** caps each scan at 64 MiB and reacts via **`CompositeOversizedPolicy`** (default: quarantine; alternatives: reject, allow-truncated). |
| **`DecompressionAlgorithmOverride`**                                                            | When set, all reads decompress with this codec instead of per-file metadata (migration/recovery).                                                                                                                                                                                                                                                                                  |

## Options — **`FileStorageServiceBaseOptions`** / **`DiskFileStorageOptions`** — Compression (resolver)

| Concern   | Behaviour                                                                                                                                                                                                                                                                                          |
|-----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Write** | When `compress: true`, **`ICompressionService.ResolveForCompress`** picks the codec (delegates to **`ICompressionAlgorithmSelector`** when registered in compression DI). **`ICompressionService.Resolver`** performs the compress; **`FileStoreResult.CompressionAlgorithm`** records the codec.  |
| **Read**  | **`metadata.CompressionAlgorithm`** → **`ICompressionService.Resolver.DecompressAsync`**. Override order: per-call `compressionAlgorithmOverride` → **`DecompressionAlgorithmOverride`** → metadata → **`ICompressionService.Algorithm`** (legacy rows with `IsCompressed` but null algorithm).    |
| **DI**    | File storage depends on **`ICompressionService`** only. Register **`AddCompressionService`** + **`AddCompressionPolicySelector`** in the host; register addon factories (LZ4, Zstd, …) for every algorithm you may **read**. See [`Lyo.Compression`](../../Compression/Lyo.Compression/README.md). |

## Options — **`FileStorageServiceBaseOptions`** / **`DiskFileStorageOptions`** — Duplicate detection

When **`EnableDuplicateDetection`** is true, saves hash plaintext and call **`IFileMetadataStore.FindByHashAsync`** before persisting transformed bytes.

| **`DuplicateStrategy`** | Behaviour                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
|-------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`ReturnExisting`**    | If an active row exists for the hash **and** the requested storage profile matches the stored row (`IsCompressed`, `IsEncrypted`, `CompressionAlgorithm` when compressed, `DataEncryptionKeyId` when encrypted — compared after write-time compression policy resolution), return that row’s metadata and skip writing. If the hash matches but the profile differs, throw **`ConflictException`** (HTTP 409). Soft-deleted rows are excluded from hash lookup. |
| **`Overwrite`**         | Delete the prior blob, reuse the existing file id, and save again using the **new** request’s compress/encrypt options (profile may change: plain ↔ compressed, unencrypted ↔ encrypted, different `keyId`, etc.).                                                                                                                                                                                                                                              |
| **`AllowDuplicate`**    | Always allocate a new file id even when the hash matches; profiles may differ.                                                                                                                                                                                                                                                                                                                                                                                  |

Reads are unchanged: **`GetFileAsync`** / **`GetFileStreamAsync`** decode according to stored metadata, not per-request compress/encrypt flags.

Legacy appsettings **`LocalFileStorage`** vs **`DiskFileStorage`** binder details are documented on **`DiskFileStorageOptions.LegacySectionName`** and *
*`DiskFileStorageConfigurationBinder`**. The internal `BindDiskFileStorage(IServiceProvider, string? preferredSection)` is invoked by the keyed-disk **`AddFileStorageServiceKeyed`
** overloads that accept a `configSectionName`; it tries the preferred section, then **`DiskFileStorageOptions.SectionName`** (`DiskFileStorage`), then *
*`DiskFileStorageOptions.LegacySectionName`** (`LocalFileStorage`) and logs a warning when the legacy section is matched.

## Keyed registration overload matrix

`AddFileStorageServiceKeyed` ships in seven shapes so the same key can wire (a) an existing keyed file store, (b) a fresh `LocalFileStorageService` from options/section, and either
reuse or define the encryption key:

| Signature                                                                                     | When to use                                                                                                   |
|-----------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| `(keyName, fileStoreKeyName, encryptionServiceKeyName)`                                       | Alias an already-registered keyed `IFileStorageService` under a new key (encryption key reused by reference). |
| `(keyName, fileStoreKeyName, configEncryptionService)`                                        | Alias an existing keyed store but provide the encryption service inline.                                      |
| `<TFileStorageService>(keyName, configFileStore, encryptionServiceKeyName)`                   | Build a new keyed file storage service from a factory; existing keyed encryption.                             |
| `<TFileStorageService>(keyName, configEncryptionService, configFileStore)`                    | Both file store and encryption built from factories.                                                          |
| `(keyName, Action<DiskFileStorageOptions>, configureMetadataStore, encryptionServiceKeyName)` | Local disk from inline options; metadata store + encryption keyed.                                            |
| `(keyName, Action<DiskFileStorageOptions>, configureMetadataStore, configEncryptionService)`  | Local disk from inline options; encryption built inline.                                                      |
| `(keyName, configSectionName, configureMetadataStore, encryptionServiceKeyName)`              | Local disk bound via `BindDiskFileStorage` from configuration; metadata + encryption keyed.                   |
| `(keyName, configSectionName, configureMetadataStore, configEncryptionService)`               | Local disk bound from configuration; encryption inline.                                                       |

For cloud backends, use the package-specific keyed entry points: **`AddS3FileStorageServiceKeyed(keyName)`** in **`Lyo.FileStorage.S3`** (fluent builder) and the non-keyed *
*`AddAzureBlobFileStorageService(...)`** in **`Lyo.FileStorage.AzureBlob`** (keyed Blob is an open item — see that README).

## DTO highlights

| Type                              | Purpose                                                                             |
|-----------------------------------|-------------------------------------------------------------------------------------|
| **`DirectUploadBeginRequest`**    | Declared max size, path prefix, content type hints for **`BeginDirectUploadAsync`** |
| **`DirectUploadBeginResult`**     | PUT URL (or SAS), TTL, **`StorageLocation`**, **`RequiredPutHeaders`**              |
| **`DirectUploadCompleteRequest`** | Expected length / rename on finalize                                                |
| **`PreSignedReadUrlOptions`**     | **`ContentDisposition`**, **`ContentType`** for cloud GET overrides                 |
| **`CopyFileRequest`**             | Optional **`PathPrefix`** override for **`CopyFileAsync`**                          |
| **`MoveFileRequest`**             | Target **`PathPrefix`** for **`MoveFileAsync`** (same file id)                      |
| **`RenameFileRequest`**           | New **`OriginalFileName`** for **`RenameFileAsync`** (metadata only)                |

Dependency injection for disk is usually **`Extensions.AddFileStorageServiceKeyed`** overloads keyed with your tenant/service key alongside **`IFileMetadataStore`** registration.

---

## Features (overview)

- **Multiple storage backends** — Local disk (**this package**); cloud in **`Lyo.FileStorage.S3`** and **`Lyo.FileStorage.AzureBlob`**
- **Compression & encryption** — Optional **`ICompressionService`** (exposes **`Resolver`** and **`ResolveForCompress`**; policy via **`AddCompressionPolicySelector`**), * *
  `ITwoKeyEncryptionService`**
- **Metadata** — **`IFileMetadataStore`** (**`FileStoreResult`**)
- **Duplicate detection** — Configurable hashing strategies (**`DuplicateHandlingStrategy`**)
- **Streaming** — **`SaveFromStreamAsync`**, pipeline reads via **`GetFileStreamAsync`**
- **Thread safety** — Design matches concurrent callers; honour lifetimes on **`IFileMetadataStore`** and keystores
- **Cleanup** — Partial file cleanup helpers on **`FileStorageServiceBase`**
- **Metrics & logging** — Hooks into **`IMetrics`** / **`ILoggerFactory`**

## Production ready

This library has been exercised for library-style production scenarios and includes defensive validation, hashing, auditing hooks, malware scan integration, and observable health
checks (**`CheckHealthAsync`**).

## Error handling

Detailed errors cover missing optional services, invalid prefixes, traversal attempts (diagnostics/listing paths), **`FileNotAvailableException`** for availability-aware reads, * *
`FilePolicyRejectedException`** for scanners, and **`FileNotFoundException`** when configured to throw.

## Security

Path prefixes are normalised on cloud/local paths via shared helpers: **`Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix`** for both listing prefixes and
save/direct-upload entry points, and **`CloudObjectKeyBuilder`** for object/blob key shape. Save paths additionally apply **`EnsureUnderRoot`** before writing to disk. * *
`HashVerifyingReadStream`** uses a fixed-time compare and only verifies on EOF. Pre-signed reads fall back to the metadata-recorded **`PathPrefix`** so SAS/GET URLs work even when
the caller cannot supply the original prefix. **`DirectUploadReceiveBaseUri`** trusts the named host — use only inside controlled (**Test API**) topologies.

## Thread safety

**`LocalFileStorageService`** and **`FileStorageServiceBase`** are safe for overlapping async calls assuming dependencies are disposed/correct lifetime.

## Health checks

Use **`await fileStorage.CheckHealthAsync(ct)`**; backends choose lightweight vs fuller modes via **`HealthCheckMode`**.

## Tests

| Project                               | Scope                                                                                                                                                                                                                |
|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`Lyo.FileStorage.Tests`**           | Local backend end-to-end (streaming, hashing, multipart, direct upload, **staged upload**, audit, scan policies, duplicate strategies, cancellation, deletion modes) plus `FileHelpers` path-prefix coverage         |
| **`Lyo.FileStorage.S3.Tests`**        | Isolated coverage for `S3UploadServerSideEncryption`, `S3UploadStream`, **`S3StagedFileUploadService`** (presigned PUT via `FakeAmazonS3`), `S3GetObjectResponseStream`, `CloudObjectKeyBuilder`, options invariants |
| **`Lyo.FileStorage.AzureBlob.Tests`** | Isolated coverage for `AzureBlobFileStorageOptions`, **`AzureBlobStagedFileUploadService`** (offline SAS generation), `CloudObjectKeyBuilder`                                                                        |

Cloud backends use a `DispatchProxy`-based lightweight stub for `IAmazonS3`; deeper end-to-end coverage would need LocalStack/Azurite.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.Compression` — (direct, lyo)
- `Lyo.ContentThreatScan` — (direct, lyo)
- `Lyo.Encryption` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.FileMetadataStore` — (direct, lyo)
- `Lyo.Hashing` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Metrics` — (direct, lyo)
- `Lyo.Streams` — (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (direct, microsoft)
- `System.Text.Json` `10.0.5` — (direct, microsoft, netstandard2.0)
- `Lyo.KeyStore` — (transitive, lyo)
- `Lyo.Result` — (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` — (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` — (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` — (transitive, third-party)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` — (transitive, microsoft)
- `System.Buffers` `4.6.1` — (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft, netstandard2.0)