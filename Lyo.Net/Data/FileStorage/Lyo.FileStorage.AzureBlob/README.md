# Lyo.FileStorage.AzureBlob

Azure Blob Storage implementation of [`IFileStorageService`](../Lyo.FileStorage/README.md) using Azure.Storage.Blobs. The package id and namespace Lyo.FileStorage.AzureBlob reflect the blob abstraction. Configuration and types use Blob* naming, with a legacy AzureFileStorageOptions appsettings subsection for migration.

Architecture, duplicate handling, and threat-model context for storage live in Lyo.FileStorage. This README is the assembly plus Azure-specific options.

## Features

- **Same contract as Local / S3.** Save, stream save, multipart, presigned reads (SAS GET), direct PUT begin/complete, server-side copy, DEK migrate/rotate, health, IFileStorageDiagnosticsService listing keys under container prefix (normalized and traversal-guarded by Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix).
- **Optional compression and two-key encryption.** Same pipeline as Lyo.FileStorage when ICompressionService / ICompressionResolver / ITwoKeyEncryptionService are registered. Reads use metadata CompressionAlgorithm via the resolver.
- **SSE.** Optional encryption scope and customer-provided key (SSE-C via base64 key on options). Applied to single-blob writes, multipart staging, header range updates, and DEK migrations. See XML docs on AzureBlobFileStorageOptions for presigned/SSE limits.
- **Multipart.** Register AddAzureBlobMultipartUploadService() after AddAzureBlobFileStorageService. Final commit uses SyncCopyFromUriAsync rather than download+re-upload.
- **Staged upload.** Register AddAzureBlobStagedFileUploadService() after blob storage. Issues SAS PUT URLs under `.stage/{stageId}/object` with required `x-ms-blob-type: BlockBlob` (and optional encryption scope headers). Not supported when CustomerProvidedKeyBase64 (SSE-C) is configured. Fail fast at BeginAsync.
- **Resolved suffix cache.** Saved metadata persists the storage extension/suffix so subsequent reads/copies skip the legacy N+1 "try base then `.gz`/`.lyo.gz`/…" probes. The shared CloudObjectKeyBuilder produces the candidate key directly.
- **Shared traversal guard.** Container/blob prefix normalization and traversal rejection live in Lyo.Exceptions.FileHelpers so the same rules apply across diagnostics listing and core save/direct-upload validation.

## Examples

### Dependency injection

```csharp
using Lyo.FileStorage.AzureBlob;
using Lyo.FileStorage.Models;

// Options instance
services.AddAzureBlobFileStorageService(new AzureBlobFileStorageOptions
{
    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    ContainerName = "my-container",
    BlobPrefix = "files"
});

// Or bind from configuration (prefers AzureBlobFileStorage, then legacy BlobFileStorage / AzureFileStorageOptions section)
services.AddAzureBlobFileStorageService(AzureBlobFileStorageOptions.SectionName);

// Multipart uploads (uses in-memory session store unless you replace it)
services.AddAzureBlobMultipartUploadService();

// Staged uploads (uses in-memory store unless Postgres/Sqlite metadata builder registered first)
services.AddAzureBlobStagedFileUploadService();
```

### Direct upload (presigned PUT)

```csharp
var url = await fileStorage.GetPreSignedReadUrlAsync(fileId, TimeSpan.FromHours(1), pathPrefix, ct);

// Optional Content-Disposition / Content-Type overrides (SAS response headers)
var urlWithHeaders = await fileStorage.GetPreSignedReadUrlAsync(
    fileId, TimeSpan.FromHours(1), pathPrefix,
    new PreSignedReadUrlOptions { ContentDisposition = "attachment", ContentType = "application/pdf" },
    ct);
```

### Configuration example

```json
{
  "BlobFileStorage": {
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    "ContainerName": "my-container",
    "BlobPrefix": "files",
    "EncryptionScope": null,
    "CustomerProvidedKeyBase64": null,
    "EnableMetrics": false,
    "HealthCheckMode": "Lightweight",
    "MaxUploadSizeBytes": 104857600
  }
}
```

## Documentation map

| Document | Scope |
| ----------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| [`Lyo.FileStorage/README.md`](../Lyo.FileStorage/README.md) | Core contracts (IFileStorageService), multipart, models, LocalFileStorageService, FileStorageServiceBaseOptions |
| This file | AzureBlobFileStorageOptions, AzureBlobFileStorageService, AddAzureBlobFileStorageService, SAS / encryption scope |
| Lyo.FileStorage.S3/README.md | S3-compatible backend (comparison for multipart and presigned behaviour) |

## AzureBlobFileStorageService

Concrete implementation registered as IFileStorageService (scoped). Mirrors S3FileStorageService for cloud-specific paths (presigned URLs, CopyFileAsync / MoveFileAsync via sync copy then delete source, multipart block uploads). RenameFileAsync is metadata-only (shared base implementation).

## AzureBlobFileStorageOptions

| Property / constant | Typical use |
| -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| SectionName (AzureBlobFileStorage) | Default configuration section |
| LegacyAzureConfigurationSectionName (AzureFileStorageOptions) | Obsolete subsections. AddAzureBlobFileStorageService(string) falls back to BlobFileStorage then AzureFileStorageOptions with a migration warning. |
| ConnectionString | Storage account connection string |
| ContainerName | Target container |
| BlobPrefix | Optional key prefix inside the container |
| EncryptionScope | Azure server-side encryption scope |
| CustomerProvidedKeyBase64 | Base64-encoded 256-bit AES key for SSE-C on SDK uploads. Direct/SAS uploads with SSE-C are not supported (fail fast). |
| EnableMetrics | When true, emits metrics if IMetrics is available |
| Inherited: HealthCheckMode, MaxUploadSizeBytes, RequireScanBeforeAvailable, duplicate/hash options, etc. | See FileStorageServiceBaseOptions xmldoc in core. |

## Dependency injection

Register Microsoft.Extensions.DependencyInjection and Microsoft.Extensions.Configuration (via your host).

> **Keyed / multi-tenant DI.** Lyo.FileStorage.AzureBlob does not currently ship the keyed builder pattern that Lyo.FileStorage.S3 exposes via AddS3FileStorageServiceKeyed. Use AddAzureBlobFileStorageService (non-keyed) per host. If you need multiple Blob backends side-by-side, register each in a separate DI scope or wire them manually via AddKeyedScoped<IFileStorageService>(...) and AddKeyedScoped<AzureBlobFileStorageService>(...) mirroring the constructor arguments of [AzureBlobFileStorageService](AzureBlobFileStorageService.cs). A keyed builder analogous to S3's is on the roadmap.

## Direct upload (presigned PUT)

BeginDirectUploadAsync returns a SAS URL plus a RequiredPutHeaders map. Clients must apply those headers verbatim on the PUT. Azure block blob uploads require at minimum `x-ms-blob-type: BlockBlob`, and when the caller supplied a ContentType the map also includes `x-ms-blob-content-type` (so the value is persisted on the blob) and Content-Type for parity with the S3 backend. Customer-Provided-Key (SSE-C) accounts are rejected at begin time. Direct PUT is not currently supported with SSE-C. Presigned reads use the core API:

## Configuration example

Legacy sections BlobFileStorage and AzureFileStorageOptions still bind if AzureBlobFileStorage is absent.

## Health

IFileStorageService extends IHealth. Call await fileStorage.CheckHealthAsync(ct) directly. Depth is controlled by HealthCheckMode.

## Concurrency

AzureBlobFileStorageService follows the core base type: concurrent calls on one instance are supported. IFileMetadataStore, keystores, and ILogger must match your DI lifetimes.

## Tests

Lyo.FileStorage.AzureBlob.Tests provides isolated unit coverage for AzureBlobFileStorageOptions (CPK resolution, section names, base defaults), AzureBlobStagedFileUploadService (offline SAS PUT generation), and the shared CloudObjectKeyBuilder. Path-prefix traversal coverage lives in Lyo.FileStorage.Tests against the shared Lyo.Exceptions.FileHelpers helper. Live container I/O would need Azurite.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Azure.Storage.Blobs` `12.29.1` (direct, third-party)
- `Lyo.ContentThreatScan` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.IO.Temp` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)