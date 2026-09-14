# Lyo.FileStorage

Save, stream-save, read, delete, and file metadata. Optional compression ([Lyo.Compression](../../Compression/Lyo.Compression/README.md)), two-key encryption ([Lyo.Encryption](../../../Security/Encryption/Lyo.Encryption/README.md)), duplicate hashing, access policies, audit hooks, multipart uploads (via [IMultipartUploadService](Multipart/IMultipartUploadService.cs)), and presigned/direct-upload/copy on cloud-capable backends.

Catalog listing is `IFileMetadataStore`. Bytes go through `IFileStorageService`. Backends that can list keys expose `IFileStoragePhysical.Physical` (`IFileSystem`). `FileStorageReconcile` joins them on file id with flags `Store` / `Physical` / `Both`.

With GenerateDocumentationFile set in Directory.Build.props, IntelliSense shows the same summaries as this README for documented members.

## Examples

### Store files on disk

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

### Store files on S3-compatible backends

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

## Where the docs live

| Document | Scope |
| ----------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| This README | IFileStorageService, LocalFileStorageService, options/DTOs, and the backend capability matrix |
| [`FileStorageArchitecture.drawio`](FileStorageArchitecture.drawio) | Multi-page diagram covering overview, upload (general), save (compress/encrypt), read, copy, DEK migrate, DEK rotate, delete |
| Lyo.FileStorage.S3/README.md: S3-compatible storage (AWS, B2, MinIO, …) | [`S3FileStorageService.cs`](../Lyo.FileStorage.S3/S3FileStorageService.cs), S3FileStorageOptions, and DI builders |
| Lyo.FileStorage.AzureBlob/README.md: Azure Blob | [`AzureBlobFileStorageService.cs`](../Lyo.FileStorage.AzureBlob/AzureBlobFileStorageService.cs), AzureBlobFileStorageOptions, plus SAS / SSE notes |
| Lyo.FileStorage.Web.Components: Blazor UI | Blazor grids and dialogs that talk to a configured Test API |

For multipart session stores and Postgres metadata, follow the references from your host registration (e.g. Lyo.FileMetadataStore.Postgres).

## What each backend can do

| Capability | Local disk (LocalFileStorageService) | S3 (S3FileStorageService) | Blob (AzureBlobFileStorageService) |
| ------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------- | ---------------------------------------- |
| Presigned GET | Only when DiskFileStorageOptions.AllowFileUriPresignedUrls (returns file://, dev-only). Response-header overrides are not supported. | Yes (incl. PreSignedReadUrlOptions) | Yes (SAS + optional response headers) |
| Direct PUT upload (BeginDirectUpload / CompleteDirectUpload) | Yes when DirectUploadReceiveBaseUri is set (the PUT URL hits the Test API / host receiver). Otherwise ConfigurationException. | Yes | Yes |
| Server-side copy (CopyFileAsync) | Yes (filesystem copy + metadata) | Yes (CopyObject) | Yes (same API) |
| Move (MoveFileAsync) | Yes (File.Move + metadata; same file id) | Yes (CopyObject then delete source) | Yes (SyncCopyFromUri then delete source) |
| Rename (RenameFileAsync) | Yes (metadata OriginalFileName only) | Yes (same) | Yes (same) |
| Diagnostics listing (IFileStorageDiagnosticsService) | Yes (relative paths under RootDirectoryPath) | Yes (combined KeyPrefix) | Yes |
| Multipart (AddLocalMultipartUploadService / …S3/Blob) | Yes (server-staged parts) | Yes | Yes |

Plaintext direct uploads (BeginDirectUpload) skip encryption and compression on the edge PUT. Finalized metadata still runs through the usual content policy and availability flags.

## CancellationToken vs cloud signing

ct honours cooperative cancellation during most async I/O. Presigned URL generation, some AWS signing, and synchronous SDK paths may still finish after cancellation is requested. See GetPreSignedReadUrlAsync and related remarks on IFileStorageService.

## IFileStorageService

- **Writes.** SaveFileAsync, SaveFromStreamAsync
- **Reads.** GetFileAsync, GetFileStreamAsync. Compressed payloads are decompressed via ICompressionService.Resolver using stored CompressionAlgorithm metadata. Optional per-call compressionAlgorithmOverride or FileStorageServiceBaseOptions.DecompressionAlgorithmOverride. Bounded by MaxDecompressedFileSize through MaxBytesWriteStream.
- **Zip archive.** IFileStorageArchiveService.CreateArchiveAsync spools files through Lyo.IO.Temp, zips on disk (nested ZipPath, caller fileName), and returns a stream that deletes the temp session on dispose. Caps: FileStorageArchiveOptions.MaxFileCount / MaxTotalUncompressedBytes. DI: AddFileStorageArchiveServiceFromConfiguration + AddFileStorageArchiveServiceKeyed.
- **Metadata / delete.** GetMetadataAsync, DeleteFileAsync(Guid, FileDeletionMode, CancellationToken). Deletes the backing object then, depending on FileDeletionMode, either tombstones metadata (RemoveObjectAndTombstoneMetadata, default) or purges it via IFileMetadataStore.PurgeMetadataAsync (RemoveObjectAndPurgeMetadata). Operator/governance flows only. Never accept this mode from end-user input.
- **Temporary links.** GetPreSignedReadUrlAsync (with optional PreSignedReadUrlOptions overrides). Both overloads accept CancellationToken, defaulting to default.
- **Direct single PUT.** BeginDirectUploadAsync, CompleteDirectUploadAsync. S3/Blob fill RequiredPutHeaders for SSE + signed Content-Type parity.
- **Copy.** CopyFileAsync (new file id)
- **Move / rename.** MoveFileAsync (same file id, relocate by PathPrefix). RenameFileAsync changes metadata OriginalFileName only.
- **Key ops.** MigrateDeksAsync, RotateDeksAsync. Results split Skipped vs Failed. Missing blobs and short encryption headers fail fast.
- **Events.** FileSaved, FileRetrieved, FileDeleted, FileMoved, FileRenamed, FileMetadataRetrieved, FileAuditOccurred. Payloads carry a redacted FileStoreSnapshot (wrapped DEK and KEK salt omitted).
- **Health.** CheckHealthAsync via IHealth. Depth follows HealthCheckMode.

## IFileStorageDiagnosticsService

Optional ListStorageKeysAsync is implemented by backends that can enumerate object/path keys (Local, S3, Blob). All implementations normalize the caller-supplied prefix through Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix, the same helper that backs FileStorageServiceBase.ValidatePathPrefix. It strips wrapping slashes and rejects `..` segments, doubled separators, and embedded `\0` with ArgumentException (HTTP 400 in ASP.NET).

## IMultipartUploadService

Large-file client-part uploads. Register multipart services together with a keyed IFileStorageService (Extensions.AddLocalMultipartUploadService or the S3/Blob equivalents).

## Types this assembly ships

| Type | Role |
| -------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| LocalFileStorageService | DiskFileStorageOptions, IFileMetadataStore, path sharding under root |
| FileStorageServiceBase | Shared pipeline for save/read/delete, hashing, auditing, and multipart plumbing |
| Extensions (namespace Lyo.FileStorage) | Keyed IFileStorageService registration for disk, plus multipart session helpers, IFileOperationContextAccessor, and IFileStorageArchiveService |
| FileStorageArchiveService | IFileStorageArchiveService: spool via Lyo.IO.Temp, nested ZipPath, caller fileName, caps from FileStorageArchiveOptions |

## Extension points

| Type | Registered via | Purpose |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| IFileOperationContextAccessor | services.AddFileOperationContextAccessor() | Async-local tenant / actor / correlation propagation is copied onto FileAuditEventArgs and policy callbacks. The default implementation is registered as a singleton. |
| IFileAuditEventHandler | Register as many as you want (services.AddScoped<IFileAuditEventHandler, MySink>()). AddPostgresFileAuditSink() in Lyo.FileMetadataStore.Postgres wires the bundled Postgres sink. | Fan-out audit handlers invoked for save / read / delete / metadata / policy events. |
| IFileContentPolicy | Register one implementation (services.AddScoped<IFileContentPolicy, MyPolicy>()). | Optional pre-save gate that can reject by content (extension/MIME/header sniffing). Rejections raise FilePolicyRejectedException. |
| IMultipartUploadSessionStore | AddInMemoryMultipartUploadSessionStore() / TryAddInMemoryMultipartUploadSessionStoreIfMissing() as the in-process default. AddPostgresMultipartUploadSessionStore() for Postgres. | Tracks staged multipart upload sessions. Local/S3/Blob multipart services require it. |

## Remote protocol backends (`Remote/`)

FTP and SFTP storage differ only in the protocol client they drive, so the storage logic lives here once and each protocol package ships a thin adapter:

- **`IRemoteFileTransport`.** The POSIX-path file operations a remote backend needs: `HealthPingAsync`, `FileExistsAsync`, `CreateDirectoryAsync`, `DeleteFileAsync`, `GetLengthAsync`, `OpenReadAsync`, `OpenCreateAsync`, `DownloadBytesAsync`, `UploadAsync`, `RenameAsync`, and `CopyFileAsync`, plus `ProtocolName` and `RootRemoteDirectory` for logs and path building. Implementations adapt a protocol library without `Lyo.FileStorage` depending on it.
- **`RemoteFileStorageServiceBase`.** A `FileStorageServiceBase` that implements every storage override (output streams, reads, deletes, size, encryption-header extraction and rewrite, partial-file cleanup, copy, move, health) against an `IRemoteFileTransport`.

Paths handed to the transport are already absolute and jailed under `RootRemoteDirectory`, so adapters do not repeat that check. Adding a protocol means implementing the transport interface and a DI extension — not another copy of the storage service.

## Multipart, direct upload, DEK operations

- FileStorageDekOperations. Implements MigrateDeksAsync and RotateDeksAsync the same way across backends, including short-encryption-header detection and per-file failure isolation.
- FileStorageStreamingPipelines. Composes compression / encryption / hash / max-size guards over streamed save and read paths.
- PlainDirectUploadCoordinator. Finalizes plaintext direct-upload PUTs into a normal SaveFileAsync outcome (policy, availability) and runs only when the caller used BeginDirectUploadAsync without compression/encryption hints.

## DiskFileStorageOptions

DiskFileStorageOptions adds:

| Property | Typical use |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| RootDirectoryPath | Root folder for blobs and bundled JSON metadata (when IFileMetadataStore is not injected explicitly) |
| EnableMetrics | Emit metrics through IMetrics when configured |
| AllowFileUriPresignedUrls | Dev only. Allow file:// presigned-style URLs rather than rejecting presigned reads. |
| DirectUploadReceiveBaseUri | Absolute origin of the host that exposes PUT …/FileStorage/direct-upload/{fileId}/put. When null, BeginDirectUploadAsync delegates to NotSupported. |
| DirectUploadPutRouteRelativePath | Path between the base URI and {fileId}/put. Default FileStorage/direct-upload. |
| Inherited (FileStorageServiceBaseOptions) | |
| HealthCheckMode | Lightweight vs deeper health probes |
| HashAlgorithm, EnableDuplicateDetection, DuplicateStrategy | Dedup by plaintext originalFileHash. See Duplicate detection. |
| ThrowOnFileNotFound, ThrowOnDeleteNotFound, ThrowOnHashMismatch | Failure-vs-null behaviour |
| MaxUploadSizeBytes, MaxDecompressedFileSize, AllowedContentTypes | Safety / validation. MaxUploadSizeBytes is enforced on direct-upload PUT bodies as well as streamed saves. An empty AllowedContentTypes list denies all uploads (configure null or omit to allow any). |
| DefaultAvailability, AllowReadQuarantinedForAdmin | DefaultAvailability is written on save when the caller does not pass an override. AllowReadQuarantinedForAdmin lets Get and presigned read open Quarantined files (admin tooling). |
| DecompressionAlgorithmOverride | When set, every read decompresses with this codec instead of per-file metadata (migration/recovery). |

## Compression resolver

| Concern | Behaviour |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Write | When compress: true, ICompressionService.ResolveForCompress picks the codec (delegates to ICompressionAlgorithmSelector when registered in compression DI). ICompressionService.Resolver performs the compress. FileStoreResult.CompressionAlgorithm records the codec. |
| Read | metadata.CompressionAlgorithm → ICompressionService.Resolver.DecompressAsync. Override order: per-call compressionAlgorithmOverride → DecompressionAlgorithmOverride → metadata → ICompressionService.Algorithm (legacy rows with IsCompressed but null algorithm). |
| DI | File storage depends on ICompressionService only. Register AddCompressionService + AddCompressionPolicySelector in the host. Register addon factories (LZ4, Zstd, …) for every algorithm you may read. See [Lyo.Compression](../../Compression/Lyo.Compression/README.md). |

## Duplicate detection

When EnableDuplicateDetection is true, saves hash plaintext and call IFileMetadataStore.FindByHashAsync before persisting transformed bytes.

| DuplicateStrategy | Behaviour |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ReturnExisting | If an active row exists for the hash and the requested storage profile matches the stored row (IsCompressed, IsEncrypted, CompressionAlgorithm when compressed, DataEncryptionKeyId when encrypted, compared after write-time compression policy resolution), return that row's metadata and skip writing. If the hash matches but the profile differs, throw ConflictException (HTTP 409). Soft-deleted rows are excluded from hash lookup. |
| Overwrite | Delete the prior blob, reuse the existing file id, and save again using the new request's compress/encrypt options (profile may change: plain ↔ compressed, unencrypted ↔ encrypted, different keyId, etc.). |
| AllowDuplicate | Always allocate a new file id even when the hash matches. Profiles may still differ. |

Reads stay the same: GetFileAsync / GetFileStreamAsync decode according to stored metadata, not per-request compress/encrypt flags.

Legacy appsettings LocalFileStorage vs DiskFileStorage binder details are documented on DiskFileStorageOptions.LegacySectionName and DiskFileStorageConfigurationBinder. The internal BindDiskFileStorage(IServiceProvider, string? preferredSection) is invoked by the keyed-disk AddFileStorageServiceKeyed overloads that accept a configSectionName. It tries the preferred section, then DiskFileStorageOptions.SectionName (DiskFileStorage), then DiskFileStorageOptions.LegacySectionName (LocalFileStorage) and logs a warning when the legacy section is matched.

## Keyed registration overload matrix

AddFileStorageServiceKeyed ships in seven shapes so the same key can wire (a) an existing keyed file store, (b) a fresh LocalFileStorageService from options/section, and either reuse or define the encryption key:

| Signature | When to use |
| --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `(keyName, fileStoreKeyName, encryptionServiceKeyName)` | Alias an already-registered keyed `IFileStorageService` under a new key (encryption key reused by reference). |
| `(keyName, fileStoreKeyName, configEncryptionService)` | Alias an existing keyed store, but provide the encryption service inline. |
| `<TFileStorageService>(keyName, configFileStore, encryptionServiceKeyName)` | Build a new keyed file storage service from a factory, reusing existing keyed encryption. |
| `<TFileStorageService>(keyName, configEncryptionService, configFileStore)` | File store and encryption both built from factories. |
| `(keyName, Action<DiskFileStorageOptions>, configureMetadataStore, encryptionServiceKeyName)` | Local disk from inline options; metadata store and encryption are keyed. |
| `(keyName, Action<DiskFileStorageOptions>, configureMetadataStore, configEncryptionService)` | Local disk from inline options; encryption is built inline. |
| `(keyName, configSectionName, configureMetadataStore, encryptionServiceKeyName)` | Local disk bound via `BindDiskFileStorage` from configuration; metadata and encryption are keyed. |
| `(keyName, configSectionName, configureMetadataStore, configEncryptionService)` | Local disk bound from configuration; encryption is inline. |

For cloud backends, use the package-specific keyed entry points: AddS3FileStorageServiceKeyed(keyName) in Lyo.FileStorage.S3 (fluent builder) and the non-keyed AddAzureBlobFileStorageService(...) in Lyo.FileStorage.AzureBlob (keyed Blob is an open item. See that README).

## DTOs

| Type | Purpose |
| --------------------------- | --------------------------------------------------------------------------------- |
| DirectUploadBeginRequest | Declared max size, path prefix, and content-type hints for BeginDirectUploadAsync |
| DirectUploadBeginResult | PUT URL (or SAS), TTL, StorageLocation, RequiredPutHeaders |
| DirectUploadCompleteRequest | Expected length / rename on finalize |
| PreSignedReadUrlOptions | ContentDisposition and ContentType for cloud GET overrides |
| CopyFileRequest | Optional PathPrefix override on CopyFileAsync |
| MoveFileRequest | Target PathPrefix for MoveFileAsync (file id stays the same) |
| RenameFileRequest | New OriginalFileName for RenameFileAsync (metadata only) |

For disk, dependency injection is usually Extensions.AddFileStorageServiceKeyed overloads keyed with your tenant/service key, alongside IFileMetadataStore registration.

## Features

- **Multiple storage backends.** Local disk lives in this package. Cloud lives in Lyo.FileStorage.S3 and Lyo.FileStorage.AzureBlob.
- **Compression and encryption.** Optional ICompressionService (exposes Resolver and ResolveForCompress; policy via AddCompressionPolicySelector) and ITwoKeyEncryptionService.
- **Metadata.** IFileMetadataStore (FileStoreResult).
- **Duplicate detection.** Configurable hashing strategies via DuplicateHandlingStrategy.
- **Streaming.** SaveFromStreamAsync, pipeline reads via GetFileStreamAsync.
- **Lifetimes.** Honour lifetimes on IFileMetadataStore and keystores. Overlapping async calls on one service instance are supported.
- **Cleanup.** Partial file cleanup helpers on FileStorageServiceBase.
- **Metrics and logging.** Hooks into IMetrics / ILoggerFactory.

## Error handling

Errors cover missing optional services, invalid prefixes, traversal attempts (diagnostics/listing paths), FileNotAvailableException for availability-aware reads, FilePolicyRejectedException for content-policy rejections, and FileNotFoundException when configured to throw.

## Security

Path prefixes are normalised on cloud/local paths via shared helpers: Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix for both listing prefixes and save/direct-upload entry points, and CloudObjectKeyBuilder for object/blob key shape (Build, FromMetadata from SourceFileName + path prefix, InferTrailingSuffixAfterFileId). Save paths also apply EnsureUnderRoot before writing to disk. HashVerifyingReadStream uses a fixed-time compare and only verifies on EOF. Pre-signed reads fall back to the metadata-recorded PathPrefix so SAS/GET URLs work even when the caller cannot supply the original prefix. DirectUploadReceiveBaseUri trusts the named host. Use only inside controlled Test API topologies.

## Concurrency

LocalFileStorageService and FileStorageServiceBase accept overlapping async calls. Honour the lifetimes of IFileMetadataStore and keystores.

## Health checks

Call await fileStorage.CheckHealthAsync(ct). Backends pick lightweight vs deeper modes via HealthCheckMode.

## Tests

| Project | Scope |
| ------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Lyo.FileStorage.Tests | Local backend end-to-end (streaming, hashing, multipart, direct upload, audit, availability policy, duplicate strategies, cancellation, deletion modes), FileHelpers path-prefix coverage, CloudObjectKeyBuilder.FromMetadata, and zip archive (flat/nested paths, names, limits, missing id, temp-session dispose) |
| Lyo.FileStorage.S3.Tests | Isolated coverage for S3UploadServerSideEncryption, S3UploadStream, S3GetObjectResponseStream, CloudObjectKeyBuilder, options invariants |
| Lyo.FileStorage.AzureBlob.Tests | Isolated coverage for AzureBlobFileStorageOptions, CloudObjectKeyBuilder |

Cloud backends use a `DispatchProxy`-based lightweight stub for `IAmazonS3`. Deeper end-to-end coverage would need LocalStack/Azurite.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.IO.FileSystem` (direct, lyo)
- `Lyo.IO.Temp` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)