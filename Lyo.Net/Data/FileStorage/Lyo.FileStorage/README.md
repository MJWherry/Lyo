# Lyo.FileStorage

Save, stream-save, read, delete, and file metadata. Optional compression ([Lyo.Compression](../../Compression/Lyo.Compression/README.md)), two-key encryption
([Lyo.Encryption](../../../Security/Encryption/Lyo.Encryption/README.md)), duplicate hashing, access policies, audit hooks, multipart uploads
(via [IMultipartUploadService](Multipart/IMultipartUploadService.cs)), and presigned/direct-upload/copy on cloud-capable backends.

Catalog listing is `IFileMetadataStore`. Bytes go through `IFileStorageService`. Backends that can list keys expose `IFileStoragePhysical.Physical` (`IFileSystem`).
`FileStorageReconcile` joins them on file id with flags `Store` / `Physical` / `Both`.

`IFileStorageTags` adds manual tags and stamps automatic `file-type` and `imported` tags. `ListTagGroupAsync` builds a virtual folder from an ordered list of tag types (including
import year, month, and day) without moving object keys. `GET capabilities` includes those tag types.

With GenerateDocumentationFile set in Directory.Build.props, IntelliSense shows the same summaries as this README for documented members.

Named stores are recorded with `AddFileStorageFactory`. Inject the scoped `IFileStorageFactory` and call `Get(name)`. Disk is built in. S3, Azure Blob, FTP, and SFTP add their own
builder methods. `FileStorage:Stores` binds `Provider`, plus optional `EncryptionServiceKey` and `CompressionServiceKey`. Omitted encryption passes null. Omitted compression uses
the unkeyed `ICompressionService`.

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

### Named disk store

```csharp
using Lyo.FileStorage;

services.AddFileStorageFactory(stores =>
{
    stores.AddDisk("photos", disk =>
    {
        disk.FromConfiguration(configuration, "FileStorage:Stores:photos");
        disk.Configure(options => options.RootDirectoryPath = "/data/photos");
        disk.UseEncryption("kek");
        disk.UseCompression("gzip");
    });
});

var photos = scope.ServiceProvider.GetRequiredService<IFileStorageFactory>().Get("photos");
```

### FileStorage:Stores

```json
{
  "FileStorage": {
    "Default": "photos",
    "Stores": {
      "photos": {
        "Provider": "Disk",
        "RootDirectoryPath": "/data/photos",
        "EncryptionServiceKey": "kek",
        "CompressionServiceKey": "gzip"
      }
    }
  }
}
```

## Where the docs live

| Document                                                                | Scope                                                                                                                                              |
|-------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| This README                                                             | IFileStorageService, LocalFileStorageService, options/DTOs, and the backend capability matrix                                                      |
| [`FileStorageArchitecture.drawio`](FileStorageArchitecture.drawio)      | Multi-page diagram covering overview, upload (general), save (compress/encrypt), read, copy, DEK migrate, DEK rotate, delete                       |
| Lyo.FileStorage.S3/README.md: S3-compatible storage (AWS, B2, MinIO, …) | [`S3FileStorageService.cs`](../Lyo.FileStorage.S3/S3FileStorageService.cs), S3FileStorageOptions, and DI builders                                  |
| Lyo.FileStorage.AzureBlob/README.md: Azure Blob                         | [`AzureBlobFileStorageService.cs`](../Lyo.FileStorage.AzureBlob/AzureBlobFileStorageService.cs), AzureBlobFileStorageOptions, plus SAS / SSE notes |
| Lyo.FileStorage.Web.Components: Blazor UI                               | Blazor grids and dialogs that talk to a configured Test API                                                                                        |

For multipart session stores and Postgres metadata, follow the references from your host registration (e.g. Lyo.FileMetadataStore.Postgres).

## What each backend can do

| Capability                                                   | Local disk (LocalFileStorageService)                                                                                                 | S3 (S3FileStorageService)            | Blob (AzureBlobFileStorageService)       |
|--------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------|------------------------------------------|
| Presigned GET                                                | Only when DiskFileStorageOptions.AllowFileUriPresignedUrls (returns file://, dev-only). Response-header overrides are not supported. | Yes (incl. PreSignedReadUrlOptions)  | Yes (SAS + optional response headers)    |
| Direct PUT upload (BeginDirectUpload / CompleteDirectUpload) | Yes when DirectUploadReceiveBaseUri is set (the PUT URL hits the Test API / host receiver). Otherwise ConfigurationException.        | Yes                                  | Yes                                      |
| Server-side copy (CopyFileAsync)                             | Yes (filesystem copy + metadata)                                                                                                     | Yes (CopyObject)                     | Yes (same API)                           |
| Move (MoveFileAsync)                                         | Yes (File.Move + metadata; same file id)                                                                                             | Yes (CopyObject then delete source)  | Yes (SyncCopyFromUri then delete source) |
| Rename (RenameFileAsync)                                     | Yes (metadata OriginalFileName only)                                                                                                 | Yes (same)                           | Yes (same)                               |
| Diagnostics listing (IFileStorageDiagnosticsService)         | Yes (relative paths under RootDirectoryPath)                                                                                         | Yes (combined KeyPrefix)             | Yes                                      |
| Multipart (AddLocalMultipartUploadService / …S3/Blob)        | Yes (server-staged parts)                                                                                                            | Yes                                  | Yes                                      |
| Byte range (OpenPlainByteRangeAsync)                         | Yes (plain slice, or encrypted from the chunk)                                                                                       | Yes (GetObject ByteRange, inclusive) | Yes (DownloadStreaming HttpRange)        |

Plaintext direct uploads (BeginDirectUpload) skip encryption and compression on the edge PUT. Finalized metadata still runs through the usual content policy and availability flags.

## CancellationToken vs cloud signing

ct honours cooperative cancellation during most async I/O. Presigned URL generation, some AWS signing, and synchronous SDK paths may still finish after cancellation is requested.
See GetPreSignedReadUrlAsync and related remarks on IFileStorageService.

## IFileStorageService

- **Writes.** SaveFileAsync, SaveFromStreamAsync
- **Reads.** GetFileAsync, GetFileStreamAsync, GetContentIntegrityAsync (plaintext hash, algorithm, and size; no wrapped key material). Compressed payloads are decompressed via
  ICompressionService.Resolver using stored CompressionAlgorithm metadata. Optional per-call compressionAlgorithmOverride or
  FileStorageServiceBaseOptions.DecompressionAlgorithmOverride. Bounded by MaxDecompressedFileSize through MaxBytesWriteStream. OpenPlainByteRangeAsync reads one inclusive slice
  when SupportsPlainByteRange is true (local disk, S3, Azure Blob). FTP and SFTP leave the flag false. A plain object is sliced at the plaintext offset. An encrypted, uncompressed
  object is decrypted from the AEAD chunk that holds that offset, not from ciphertext byte 0. Compressed objects throw. The slice is not hash-verified.
- **Zip archive.** IFileStorageArchiveService.CreateArchiveAsync spools files through Lyo.IO.Temp, zips on disk (nested ZipPath, caller fileName), and returns a stream that deletes
  the temp session on dispose. Caps: FileStorageArchiveOptions.MaxFileCount / MaxTotalUncompressedBytes. DI: AddFileStorageArchiveServiceFromConfiguration +
  AddFileStorageArchiveServiceKeyed.
- **Metadata / delete.** GetMetadataAsync, DeleteFileAsync (Guid, FileDeletionMode, CancellationToken). Deletes the backing object then, depending on FileDeletionMode, either
  tombstones metadata (RemoveObjectAndTombstoneMetadata, default) or purges it via IFileMetadataStore.PurgeMetadataAsync (RemoveObjectAndPurgeMetadata). Operator/governance flows
  only. Never accept this mode from end-user input.
- **Temporary links.** GetPreSignedReadUrlAsync (with optional PreSignedReadUrlOptions overrides). Both overloads accept CancellationToken, defaulting to default.
- **Direct single PUT.** BeginDirectUploadAsync, CompleteDirectUploadAsync. S3/Blob fill RequiredPutHeaders for SSE + signed Content-Type parity.
- **Copy.** CopyFileAsync (new file id)
- **Move / rename.** MoveFileAsync (same file id, relocate by PathPrefix). RenameFileAsync changes metadata OriginalFileName only.
- **Key ops.** MigrateDeksAsync, RotateDeksAsync. Results split Skipped vs Failed. Missing blobs and short encryption headers fail fast.
- **Events.** FileSaved, FileRetrieved, FileDeleted, FileMoved, FileRenamed, FileMetadataRetrieved, FileAuditOccurred. Payloads carry a redacted FileStoreSnapshot (wrapped DEK and
  KEK salt omitted).
- **Health.** CheckHealthAsync via IHealth. Depth follows HealthCheckMode.
- **Capabilities.** GetCapabilities reports presigned read, direct upload, copy, move, and physical listing for this backend. Local presign follows AllowFileUriPresignedUrls. Local
  direct upload follows DirectUploadReceiveBaseUri. HTTP hosts add multipart, encryption, archive, and access-link flags on GET capabilities.

## IFileStorageDiagnosticsService

Optional ListStorageKeysAsync is implemented by backends that can enumerate object/path keys (Local, S3, Blob). All implementations normalize the caller-supplied prefix through
Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix, the same helper that backs FileStorageServiceBase.ValidatePathPrefix. It strips wrapping slashes and rejects `..`
segments, doubled separators, and embedded `\0` with ArgumentException (HTTP 400 in ASP.NET).

## IMultipartUploadService

Large-file client-part uploads. Register multipart services together with a keyed IFileStorageService (Extensions.AddLocalMultipartUploadService or the S3/Blob equivalents).

## Types this assembly ships

| Type                                   | Role                                                                                                                                                                                                                                                                                                                            |
|----------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| LocalFileStorageService                | DiskFileStorageOptions, IFileMetadataStore, path sharding under root                                                                                                                                                                                                                                                            |
| FileStorageServiceBase                 | Shared pipeline for save/read/delete, hashing, auditing, and multipart plumbing                                                                                                                                                                                                                                                 |
| Extensions (namespace Lyo.FileStorage) | Keyed IFileStorageService registration for disk, plus multipart session helpers, IFileOperationContextAccessor, and IFileStorageArchiveService                                                                                                                                                                                  |
| FileStorageArchiveService              | IFileStorageArchiveService: spool via Lyo.IO.Temp, nested ZipPath, caller fileName, caps from FileStorageArchiveOptions                                                                                                                                                                                                         |
| FileStorageTransfer                    | Copies or moves files between two IFileStorageService instances. KeysOnly copies referenced key-encryption keys. Everything also copies the selected objects. PreserveKeys copies ciphertext. NewKey and ExistingKey re-save with the same compression and encryption flags. Move tombstones the source after the hashes match. |

## Extension points

| Type                          | Registered via                                                                                                                                                                     | Purpose                                                                                                                                                                                                                                                                |
|-------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| IFileOperationContextAccessor | services.AddFileOperationContextAccessor()                                                                                                                                         | Async-local tenant / actor / correlation propagation is copied onto FileAuditEventArgs and policy callbacks. The default implementation is registered as a singleton.                                                                                                  |
| IFileAuditEventHandler        | Register as many as you want (services.AddScoped<IFileAuditEventHandler, MySink>()). AddPostgresFileAuditSink() in Lyo.FileMetadataStore.Postgres wires the bundled Postgres sink. | Fan-out audit handlers invoked for save / read / delete / metadata / policy events.                                                                                                                                                                                    |
| IFileContentPolicy            | Register one implementation (services.AddScoped<IFileContentPolicy, MyPolicy>()).                                                                                                  | Optional pre-save gate. The default policy enforces the content-type and file-name lists. Rejections raise FilePolicyRejectedException. A replacement policy skips those lists on save; rename and direct-upload complete still apply the file-name list.              |
| IFileTypeSniffer              | Optional. Storage factories pass services.GetService<IFileTypeSniffer>(). The default is MagicFileTypeSniffer.                                                                     | Identifies a file from its header. Used when VerifyFileSignature is true or a sniffed-type list is set. FileSignatureAlignment rejects a header that disagrees with the claimed type. FileSniffedTypeFilter applies AllowedSniffedFileTypes or DeniedSniffedFileTypes. |
| IMultipartUploadSessionStore  | AddInMemoryMultipartUploadSessionStore() / TryAddInMemoryMultipartUploadSessionStoreIfMissing() as the in-process default. AddPostgresMultipartUploadSessionStore() for Postgres.  | Tracks staged multipart upload sessions. Local/S3/Blob multipart services require it.                                                                                                                                                                                  |

## Remote protocol backends (`Remote/`)

FTP and SFTP storage differ only in the protocol client they drive, so the storage logic lives here once and each protocol package ships a thin adapter:

- **`IRemoteFileTransport`.** The POSIX-path file operations a remote backend needs: `HealthPingAsync`, `FileExistsAsync`, `CreateDirectoryAsync`, `DeleteFileAsync`,
  `GetLengthAsync`, `OpenReadAsync`, `OpenCreateAsync`, `DownloadBytesAsync`, `UploadAsync`, `RenameAsync`, and `CopyFileAsync`, plus `ProtocolName` and `RootRemoteDirectory` for
  logs and path building. Implementations adapt a protocol library without `Lyo.FileStorage` depending on it.
- **`RemoteFileStorageServiceBase`.** A `FileStorageServiceBase` that implements every storage override (output streams, reads, deletes, size, encryption-header extraction and
  rewrite, partial-file cleanup, copy, move, health) against an `IRemoteFileTransport`.

Paths handed to the transport are already absolute and jailed under `RootRemoteDirectory`, so adapters do not repeat that check. Adding a protocol means implementing the transport
interface and a DI extension — not another copy of the storage service.

## Multipart, direct upload, DEK operations

- FileStorageDekOperations. Implements MigrateDeksAsync and RotateDeksAsync the same way across backends, including short-encryption-header detection and per-file failure
  isolation.
- FileStorageStreamingPipelines. Composes compression / encryption / hash / max-size guards over streamed save and read paths.
- PlainDirectUploadCoordinator. Finalizes plaintext direct-upload PUTs into a normal SaveFileAsync outcome (policy, availability) and runs only when the caller used
  BeginDirectUploadAsync without compression/encryption hints.
- FileStorageTransfer. Copies or moves files onto another backend. MigrateDeksAsync is unchanged: it rewraps data-encryption keys inside one backend and does not move blobs or copy
  a key store.

## Copying or moving files (`FileStorageTransfer`)

`FileStorageTransfer.TransferAsync` takes a source endpoint, a destination endpoint, and a request. Each endpoint is the storage service, the metadata catalog it was constructed
with, and its `IKeyStore`. `Copy` leaves the source in place. `Move` tombstones a source file only after the destination hashes match, and it never deletes source keys. Move
requires a separate catalog. A shared catalog can still be copied.

`KeysOnly` copies the key versions referenced by the selected rows and does not read or write blobs. `Everything` also copies those objects. Pass `FileIds` to transfer that set.
Null selects the catalog. An empty set, a missing id, a tombstone, or a pending direct upload throws before any write. `Move` with `KeysOnly` throws before any write.

`PreserveKeys` copies raw objects and the same key-encryption keys. Object keys come from `CloudObjectKeyBuilder.FromMetadata` with no storage prefix, because
`IFileStoragePhysical.Physical` is already jailed to the disk root or bucket prefix. A shared catalog is left unchanged; a separate catalog receives the same `FileStoreResult`.
`NewKey` mints a 32-byte key and re-saves every file with the source compression and encryption flags. `ExistingKey` re-saves under a key the destination already has and does not
write key material. `KeysOnly` with either re-encrypt mode throws before any write. Re-encrypt also throws when both endpoints share one metadata instance.

A list that comes back full (`Count == ListCap`) throws before any write. `ListCap` applies only to the catalog list. Tombstones and pending direct uploads are not copied from a
catalog list. A file is skipped only when the recomputed destination hashes match. A different stored `OriginalFileHash` fails that file and does not overwrite it. A missing or
different stored object with the same metadata hash is rewritten, then checked again. `PreserveKeys` checks the plaintext hash and `SourceFileHash` (the bytes on disk). `NewKey`
and `ExistingKey` check the plaintext hash, and the destination compression and encryption flags must match the source. A file the service will not read, such as `PendingScan`,
still copies under `PreserveKeys` when the stored-object hash matches. `FilesFailed > 0` means the run did not fully succeed. Earlier destination files stay. A failed file stays on
the source. Cancellation throws `OperationCanceledException`. Pass `IProgress<FileStorageTransferProgress>` to hear listing, key, file, and throttled byte progress. Null progress
reports nothing. Raw copy progress uses the stored size. Re-save progress uses the plaintext size.

`GetContentIntegrityAsync` returns the plaintext hash, algorithm, and size without wrapped key material. That is the hash of a full download body.

FTP and SFTP do not implement `IFileStoragePhysical`, so `PreserveKeys` throws for them. They can still use `NewKey` or `ExistingKey`.

## DiskFileStorageOptions

DiskFileStorageOptions adds:

| Property                                                                                                                              | Typical use                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|---------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| RootDirectoryPath                                                                                                                     | Root folder for blobs and bundled JSON metadata (when IFileMetadataStore is not injected explicitly)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| EnableMetrics                                                                                                                         | Emit metrics through IMetrics when configured                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| AllowFileUriPresignedUrls                                                                                                             | Dev only. Allow file:// presigned-style URLs rather than rejecting presigned reads.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| DirectUploadReceiveBaseUri                                                                                                            | Absolute origin of the host that exposes PUT …/FileStorage/direct-upload/{fileId}/put. When null, BeginDirectUploadAsync delegates to NotSupported.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| DirectUploadPutRouteRelativePath                                                                                                      | Path between the base URI and {fileId}/put. Default FileStorage/direct-upload.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Inherited (FileStorageServiceBaseOptions)                                                                                             |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| HealthCheckMode                                                                                                                       | Lightweight vs deeper health probes                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| HashAlgorithm, EnableDuplicateDetection, DuplicateStrategy                                                                            | Dedup by plaintext originalFileHash. See Duplicate detection.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| ThrowOnFileNotFound, ThrowOnDeleteNotFound, ThrowOnHashMismatch                                                                       | Failure-vs-null behaviour                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| MaxUploadSizeBytes, MaxDecompressedFileSize, Allowed/Denied content types, file-name patterns, and sniffed types, VerifyFileSignature | Safety / validation. MaxUploadSizeBytes is enforced on direct-upload PUT bodies as well as streamed saves. For content type, file name, and sniffed type, set Allowed* (whitelist) or Denied* (blacklist), not both — ValidateUploadFilters throws ArgumentException when both sides of one pair are non-null. Null omits that side. An empty whitelist rejects everything on that dimension; an empty blacklist rejects nothing. VerifyFileSignature starts false; when true, the header must agree with the claimed type. Sniffed names are FileTypeInfo names. JAR, DOCX, and XLSX share the ZIP signature, so denying JAR denies every PK header. |
| DefaultAvailability, AllowReadQuarantinedForAdmin                                                                                     | DefaultAvailability is written on save when the caller does not pass an override. AllowReadQuarantinedForAdmin lets Get and presigned read open Quarantined files (admin tooling).                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| DecompressionAlgorithmOverride                                                                                                        | When set, every read decompresses with this codec instead of per-file metadata (migration/recovery).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |

## Upload filters

Each filter is a whitelist (`Allowed*`) or a blacklist (`Denied*`). Leave a property unset to skip that side. Setting both sides of one filter throws at startup.

```json
"DiskFileStorage": {
  "RootDirectoryPath": "/data/files",
  "AllowedContentTypes": [ "application/pdf", "image/png", "image/jpeg" ],
  "DeniedFileNamePatterns": [ "\\.(exe|dll|jar|bat|cmd|ps1|scr)$" ],
  "DeniedSniffedFileTypes": [ "ZIP", "RAR", "7Z" ],
  "VerifyFileSignature": true
}
```

File-name patterns match the full name and are not auto-anchored, so `\.pdf$` allows `report.pdf` and rejects `report.pdf.jar`. An unrecognized header fails a sniffed whitelist and
passes a sniffed blacklist. `VerifyFileSignature` rejects `report.pdf` whose bytes start with `PK`.

## Compression resolver

| Concern | Behaviour                                                                                                                                                                                                                                                                  |
|---------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Write   | When compress: true, ICompressionService.ResolveForCompress picks the codec (delegates to ICompressionAlgorithmSelector when registered in compression DI). ICompressionService.Resolver performs the compress. FileStoreResult.CompressionAlgorithm records the codec.    |
| Read    | metadata.CompressionAlgorithm → ICompressionService.Resolver.DecompressAsync. Override order: per-call compressionAlgorithmOverride → DecompressionAlgorithmOverride → metadata → ICompressionService.Algorithm (legacy rows with IsCompressed but null algorithm).        |
| DI      | File storage depends on ICompressionService only. Register AddCompressionService + AddCompressionPolicySelector in the host. Register addon factories (LZ4, Zstd, …) for every algorithm you may read. See [Lyo.Compression](../../Compression/Lyo.Compression/README.md). |

## Duplicate detection

When EnableDuplicateDetection is true, saves hash plaintext and call IFileMetadataStore.FindByHashAsync before persisting transformed bytes.

| DuplicateStrategy | Behaviour                                                                                                                                                                                                                                                                                                                                                                                                                                    |
|-------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ReturnExisting    | If an active row exists for the hash and the requested storage profile matches the stored row (IsCompressed, IsEncrypted, CompressionAlgorithm when compressed, DataEncryptionKeyId when encrypted, compared after write-time compression policy resolution), return that row's metadata and skip writing. If the hash matches but the profile differs, throw ConflictException (HTTP 409). Soft-deleted rows are excluded from hash lookup. |
| Overwrite         | Delete the prior blob, reuse the existing file id, and save again using the new request's compress/encrypt options (profile may change: plain ↔ compressed, unencrypted ↔ encrypted, different keyId, etc.).                                                                                                                                                                                                                                 |
| AllowDuplicate    | Always allocate a new file id even when the hash matches. Profiles may still differ.                                                                                                                                                                                                                                                                                                                                                         |

Reads stay the same: GetFileAsync / GetFileStreamAsync decode according to stored metadata, not per-request compress/encrypt flags.

Disk options bind from the preferred section, then DiskFileStorage.

## Keyed registration overload matrix

AddFileStorageServiceKeyed ships in seven shapes so the same key can wire (a) an existing keyed file store, (b) a fresh LocalFileStorageService from options/section, and either
reuse or define the encryption key:

| Signature                                                                                     | When to use                                                                                                   |
|-----------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------|
| `(keyName, fileStoreKeyName, encryptionServiceKeyName)`                                       | Alias an already-registered keyed `IFileStorageService` under a new key (encryption key reused by reference). |
| `(keyName, fileStoreKeyName, configEncryptionService)`                                        | Alias an existing keyed store, but provide the encryption service inline.                                     |
| `<TFileStorageService>(keyName, configFileStore, encryptionServiceKeyName)`                   | Build a new keyed file storage service from a factory, reusing existing keyed encryption.                     |
| `<TFileStorageService>(keyName, configEncryptionService, configFileStore)`                    | File store and encryption both built from factories.                                                          |
| `(keyName, Action<DiskFileStorageOptions>, configureMetadataStore, encryptionServiceKeyName)` | Local disk from inline options; metadata store and encryption are keyed.                                      |
| `(keyName, Action<DiskFileStorageOptions>, configureMetadataStore, configEncryptionService)`  | Local disk from inline options; encryption is built inline.                                                   |
| `(keyName, configSectionName, configureMetadataStore, encryptionServiceKeyName)`              | Local disk bound via `BindDiskFileStorage` from configuration; metadata and encryption are keyed.             |
| `(keyName, configSectionName, configureMetadataStore, configEncryptionService)`               | Local disk bound from configuration; encryption is inline.                                                    |

For cloud backends, use the package-specific keyed entry points: AddS3FileStorageServiceKeyed (keyName) in Lyo.FileStorage.S3 (fluent builder) and the non-keyed
AddAzureBlobFileStorageService (...) in Lyo.FileStorage.AzureBlob (keyed Blob is an open item. See that README).

## DTOs

| Type                        | Purpose                                                                           |
|-----------------------------|-----------------------------------------------------------------------------------|
| DirectUploadBeginRequest    | Declared max size, path prefix, and content-type hints for BeginDirectUploadAsync |
| DirectUploadBeginResult     | PUT URL (or SAS), TTL, StorageLocation, RequiredPutHeaders                        |
| DirectUploadCompleteRequest | Expected length / rename on finalize                                              |
| PreSignedReadUrlOptions     | ContentDisposition and ContentType for cloud GET overrides                        |
| CopyFileRequest             | Optional PathPrefix override on CopyFileAsync                                     |
| MoveFileRequest             | Target PathPrefix for MoveFileAsync (file id stays the same)                      |
| RenameFileRequest           | New OriginalFileName for RenameFileAsync (metadata only)                          |

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

Errors cover missing optional services, invalid prefixes, traversal attempts (diagnostics/listing paths), FileNotAvailableException for availability-aware reads,
FilePolicyRejectedException for content-policy rejections, and FileNotFoundException when configured to throw.

## Security

Path prefixes are normalised on cloud/local paths via shared helpers: Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix for both listing prefixes and save/direct-upload
entry points, and CloudObjectKeyBuilder for object/blob key shape (Build, FromMetadata from SourceFileName + path prefix, InferTrailingSuffixAfterFileId). Save paths also apply
EnsureUnderRoot before writing to disk. HashVerifyingReadStream uses a fixed-time compare and only verifies on EOF. Pre-signed reads fall back to the metadata-recorded PathPrefix
so SAS/GET URLs work even when the caller cannot supply the original prefix. DirectUploadReceiveBaseUri trusts the named host. Use only inside controlled Test API topologies.

## Concurrency

LocalFileStorageService and FileStorageServiceBase accept overlapping async calls. Honour the lifetimes of IFileMetadataStore and keystores.

## Health checks

Call await fileStorage.CheckHealthAsync (ct). Backends pick lightweight vs deeper modes via HealthCheckMode.

## Tests

| Project                         | Scope                                                                                                                                                                                                                                                                                                               |
|---------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Lyo.FileStorage.Tests           | Local backend end-to-end (streaming, hashing, multipart, direct upload, audit, availability policy, duplicate strategies, cancellation, deletion modes), FileHelpers path-prefix coverage, CloudObjectKeyBuilder.FromMetadata, and zip archive (flat/nested paths, names, limits, missing id, temp-session dispose) |
| Lyo.FileStorage.S3.Tests        | Isolated coverage for S3UploadServerSideEncryption, S3UploadStream, S3GetObjectResponseStream, CloudObjectKeyBuilder, options invariants                                                                                                                                                                            |
| Lyo.FileStorage.AzureBlob.Tests | Isolated coverage for AzureBlobFileStorageOptions, CloudObjectKeyBuilder                                                                                                                                                                                                                                            |

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
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Streams` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Core` (transitive, lyo)
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