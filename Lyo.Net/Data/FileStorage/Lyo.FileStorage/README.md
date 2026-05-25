# Lyo.FileStorage

Production-oriented **file storage** for .NET: save / stream-save / read / delete / metadata with optional **compression** ([`Lyo.Compression`](../../Compression/Lyo.Compression/README.md)), **two-key encryption**
([`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)), **duplicate hashing**, **access policies**, **malware scans**, **audit hooks**, **multipart uploads** (via
[`IMultipartUploadService`](Multipart/IMultipartUploadService.cs)), and **presigned/direct-upload/copy** on cloud-capable backends.

With XML documentation generation enabled in the repo (**`GenerateDocumentationFile`** in **`Directory.Build.props`**), IntelliSense surfaces the same summaries as this README for documented members.

## Documentation map

| Document | Scope |
|----------|--------|
| **This README** | Core **`IFileStorageService`**, **`LocalFileStorageService`**, options/DTOs, backend capability matrix |
| [`FileStorageArchitecture.drawio`](FileStorageArchitecture.drawio) | Three-page diagram: overview (split abstractions + persisted concerns), upload flow (save / multipart / direct PUT), delete flow (tombstone-first) |
| **`Lyo.FileStorage.S3/README.md`**: S3-compatible storage (AWS, B2, MinIO, …) | [`S3FileStorageService.cs`](../Lyo.FileStorage.S3/S3FileStorageService.cs), **`S3FileStorageOptions`**, DI builders |
| **`Lyo.FileStorage.Blob/README.md`**: Azure Blob | [`BlobFileStorageService.cs`](../Lyo.FileStorage.Blob/BlobFileStorageService.cs), **`BlobFileStorageOptions`**, SAS / SSE notes |
| **`Lyo.FileStorage.Web.Components`**: Workbench UI | Blazor grids and dialogs that call a configured Test API |

For multipart session stores and Postgres metadata, follow references from your host registration (e.g. **`Lyo.FileMetadataStore.Postgres`**).

## Backend capability matrix

| Capability | Local disk (**`LocalFileStorageService`**) | S3 (**`S3FileStorageService`**) | Blob (**`BlobFileStorageService`**) |
|------------|--------------------------------------------|--------------------------------|-------------------------------------|
| **Presigned GET** | Only when **`DiskFileStorageOptions.AllowFileUriPresignedUrls`** (returns **`file://`**, dev-only); no response-header overrides | Yes (incl. **`PreSignedReadUrlOptions`**) | Yes (SAS + optional response headers) |
| **Direct PUT upload** (**`BeginDirectUpload` / `CompleteDirectUpload`**) | Yes when **`DirectUploadReceiveBaseUri`** is set (PUT URL hits Test API / host receiver); otherwise **`NotSupportedException`** | Yes | Yes |
| **Server-sideCopy** (**`CopyFileAsync`**) | Yes (filesystem copy + metadata) | Yes (`CopyObject`) | Yes (same API) |
| **Diagnostics listing** (**`IFileStorageDiagnosticsService`**) | Yes (relative paths under **`RootDirectoryPath`**) | Yes (combined **`KeyPrefix`**) | Yes |
| **Multipart** (**`AddLocalMultipartUploadService` / …S3/Blob**) | Yes (server-staged parts) | Yes | Yes |

Plaintext direct uploads (**`BeginDirectUpload`**) deliberately **exclude** encryption/compression on the edge PUT; finalized metadata runs through normal policy/malware/availability flags.

### **`CancellationToken`** and cloud signing

**`ct`** honours cooperative cancellation during most async I/O. **Presigned URL generation**, some **AWS signing**, and synchronous SDK paths may complete even after cancellation is requested — see **`GetPreSignedReadUrlAsync`** and related remarks on **`IFileStorageService`**.

## Contracts

### **`IFileStorageService`**

Grouped by concern (see xmldoc for parameters and exceptions):

- **Writes** — **`SaveFileAsync`**, **`SaveFromStreamAsync`**
- **Reads** — **`GetFileAsync`**, **`GetFileStreamAsync`** (compressed payloads are unwrapped through **`MaxBytesWriteStream`** to reject decompression bombs above **`MaxDecompressedFileSize`**)
- **Metadata / delete** — **`GetMetadataAsync`**, **`DeleteFileAsync`** (tombstones metadata before object delete)
- **Temporary links** — **`GetPreSignedReadUrlAsync`** (with optional **`PreSignedReadUrlOptions`** overrides; both overloads accept **`CancellationToken`** with a default of `default`)
- **Direct single PUT** — **`BeginDirectUploadAsync`**, **`CompleteDirectUploadAsync`** (S3/Blob populate **`RequiredPutHeaders`** for SSE + signed `Content-Type` parity)
- **Copy** — **`CopyFileAsync`**
- **Key ops** — **`MigrateDeksAsync`**, **`RotateDeksAsync`** (results split **`Skipped`** vs **`Failed`**; missing blobs and short encryption headers fail-fast)
- **Events** — **`FileSaved`**, **`FileRetrieved`**, **`FileDeleted`**, **`FileMetadataRetrieved`**, **`FileAuditOccurred`**. Payloads carry a redacted **`FileStoreSnapshot`** (wrapped DEK and KEK salt omitted)
- **Health** — **`CheckHealthAsync`** via **`IHealth`** (lightweight vs full via **`HealthCheckMode`**)

### **`IFileStorageDiagnosticsService`**

Optional **`ListStorageKeysAsync`** implemented by backends that can enumerate object/path keys (Local, S3, Blob). All implementations normalize the caller-supplied prefix through **`Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix`** — the same helper that backs **`FileStorageServiceBase.ValidatePathPrefix`** — which strips wrapping slashes and rejects `..` segments, doubled separators, and embedded `\0` with **`ArgumentException`** (HTTP 400 in ASP.NET).

### **`IMultipartUploadService`**

Large-file client-part uploads; register multipart services together with keyed **`IFileStorageService`** (**`Extensions.AddLocalMultipartUploadService`** or S3/Blob equivalents).

## Service matrix (**`Lyo.FileStorage`** assembly)

| Type | Role |
|------|------|
| **`LocalFileStorageService`** | **`DiskFileStorageOptions`**, **`IFileMetadataStore`**, path sharding under root |
| **`FileStorageServiceBase`** | Shared pipeline for save/read/delete, hashing, auditing, multipart plumbing |
| **`Extensions`** — namespace **`Lyo.FileStorage`** | Keyed **`IFileStorageService`** registration for disk, multipart session helpers, **`IFileOperationContextAccessor`** |

## Options — **`FileStorageServiceBaseOptions`** / **`DiskFileStorageOptions`**

**`DiskFileStorageOptions`** adds:

| Property | Typical use |
|----------|-------------|
| **`RootDirectoryPath`** | Root folder for blobs and bundled JSON metadata (when **`IFileMetadataStore`** not injected explicitly) |
| **`EnableMetrics`** | Emit metrics via **`IMetrics`** when configured |
| **`AllowFileUriPresignedUrls`** | (**Dev**) allow **`file://`** presigned-style URLs instead of rejecting presigned reads |
| **`DirectUploadReceiveBaseUri`** | Absolute origin of the host that exposes **`PUT …/Workbench/FileStorage/direct-upload/{fileId}/put`** (matches **Lyo.TestApi** conventions). When null, **`BeginDirectUploadAsync`** delegates to **`NotSupported`** |
| **`DirectUploadPutRouteRelativePath`** | Path between base URI and **`{fileId}/put`**; default **`Workbench/FileStorage/direct-upload`** with the bundled Test API |
| **Inherited (`FileStorageServiceBaseOptions`)**

| **`HealthCheckMode`** | Lightweight vs fuller health probes |

| **`HashAlgorithm`**, **`EnableDuplicateDetection`**, **`DuplicateStrategy`** | Dedup semantics |

| **`ThrowOnFileNotFound`**, **`ThrowOnDeleteNotFound`**, **`ThrowOnHashMismatch`** | Failure-vs-null behaviour |

| **`MaxUploadSizeBytes`**, **`MaxDecompressedFileSize`**, **`AllowedContentTypes`** | Safety / validation. `MaxUploadSizeBytes` is enforced on direct-upload PUT bodies in addition to streamed saves; an **empty** `AllowedContentTypes` list **denies** all uploads (configure null or omit to allow any). |

| **`RequireScanBeforeAvailable`**, **`DefaultAvailability`**, **`AllowReadQuarantinedForAdmin`** | Availability + **`IFileMalwareScanner`** integration. When `RequireScanBeforeAvailable` is true and no scanner is registered, saves **fail-closed** across `byte[]`/stream/direct-upload paths. The chained **`CompositeFileMalwareScanner`** caps each scan at 64 MiB and reacts via **`CompositeOversizedPolicy`** (default: quarantine; alternatives: reject, allow-truncated). |

Legacy appsettings **`LocalFileStorage`** vs **`DiskFileStorage`** binder details are documented on **`DiskFileStorageOptions.LegacySectionName`** and **`DiskFileStorageConfigurationBinder`**.

## DTO highlights

| Type | Purpose |
|------|---------|
| **`DirectUploadBeginRequest`** | Declared max size, path prefix, content type hints for **`BeginDirectUploadAsync`** |
| **`DirectUploadBeginResult`** | PUT URL (or SAS), TTL, **`StorageLocation`**, **`RequiredPutHeaders`** |
| **`DirectUploadCompleteRequest`** | Expected length / rename on finalize |
| **`PreSignedReadUrlOptions`** | **`ContentDisposition`**, **`ContentType`** for cloud GET overrides |
| **`CopyFileRequest`** | Optional **`PathPrefix`** override for **`CopyFileAsync`** |

Dependency injection for disk is usually **`Extensions.AddFileStorageServiceKeyed`** overloads keyed with your tenant/service key alongside **`IFileMetadataStore`** registration.

---

## Features (overview)

- **Multiple storage backends** — Local disk (**this package**); cloud in **`Lyo.FileStorage.S3`** and **`Lyo.FileStorage.Blob`**
- **Compression & encryption** — Optional **`ICompressionService`**, **`ITwoKeyEncryptionService`**
- **Metadata** — **`IFileMetadataStore`** (**`FileStoreResult`**)
- **Duplicate detection** — Configurable hashing strategies (**`DuplicateHandlingStrategy`**)
- **Streaming** — **`SaveFromStreamAsync`**, pipeline reads via **`GetFileStreamAsync`**
- **Thread safety** — Design matches concurrent callers; honour lifetimes on **`IFileMetadataStore`** and keystores
- **Cleanup** — Partial file cleanup helpers on **`FileStorageServiceBase`**
- **Metrics & logging** — Hooks into **`IMetrics`** / **`ILoggerFactory`**

## Quick start

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

## Production ready

This library has been exercised for library-style production scenarios and includes defensive validation, hashing, auditing hooks, malware scan integration, and observable health checks (**`CheckHealthAsync`**).

## Error handling

Detailed errors cover missing optional services, invalid prefixes, traversal attempts (diagnostics/listing paths), **`FileNotAvailableException`** for availability-aware reads, **`FilePolicyRejectedException`** for scanners, and **`FileNotFoundException`** when configured to throw.

## Security

Path prefixes are normalised on cloud/local paths via shared helpers: **`Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix`** for both listing prefixes and save/direct-upload entry points, and **`CloudObjectKeyBuilder`** for object/blob key shape. Save paths additionally apply **`EnsureUnderRoot`** before writing to disk. **`HashVerifyingReadStream`** uses a fixed-time compare and only verifies on EOF. Pre-signed reads fall back to the metadata-recorded **`PathPrefix`** so SAS/GET URLs work even when the caller cannot supply the original prefix. **`DirectUploadReceiveBaseUri`** trusts the named host — use only inside controlled (**Test API**) topologies.

## Thread safety

**`LocalFileStorageService`** and **`FileStorageServiceBase`** are safe for overlapping async calls assuming dependencies are disposed/correct lifetime.

## Health checks

Use **`await fileStorage.CheckHealthAsync(ct)`**; backends choose lightweight vs fuller modes via **`HealthCheckMode`**.

## Tests

| Project | Scope |
|---------|-------|
| **`Lyo.FileStorage.Tests`** | Local backend end-to-end (streaming, hashing, multipart, direct upload, audit, scan policies, duplicate strategies, cancellation, deletion modes) plus `FileHelpers` path-prefix coverage |
| **`Lyo.FileStorage.S3.Tests`** | Isolated coverage for `S3UploadServerSideEncryption`, `S3UploadStream` (single PUT + multipart + abort + SSE forwarding), `S3GetObjectResponseStream`, `CloudObjectKeyBuilder`, options invariants |
| **`Lyo.FileStorage.Blob.Tests`** | Isolated coverage for `BlobFileStorageOptions` (CPK, section names), `CloudObjectKeyBuilder` |

Cloud backends use a `DispatchProxy`-based lightweight stub for `IAmazonS3`; deeper end-to-end coverage would need LocalStack/Azurite.

## Dependencies

*(From `Lyo.FileStorage.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

_None declared beyond the BCL in this project file._

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Compression`](../../Compression/Lyo.Compression/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.FileMetadataStore`](../../FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.Health`](../../../Core/Health/Lyo.Health/README.md)
- [`Lyo.Metrics`](../../../Core/Metrics/Lyo.Metrics/README.md)
- [`Lyo.Streams`](../../../Core/Streams/Lyo.Streams/README.md)
