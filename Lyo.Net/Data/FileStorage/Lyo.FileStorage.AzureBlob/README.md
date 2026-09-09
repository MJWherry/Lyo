# Lyo.FileStorage.AzureBlob

Azure.Storage.Blobs [`IFileStorageService`](../Lyo.FileStorage/README.md) for Azure Blob Storage. The package id and namespace Lyo.FileStorage.AzureBlob match the blob abstraction. Configuration and types use Blob* naming, with a legacy AzureFileStorageOptions appsettings subsection for migration.

Architecture, duplicate handling, and threat-model context for storage live in Lyo.FileStorage. This README is the assembly plus Azure-specific options.

## Features

- **Same contract as Local / S3.** Save, stream save, multipart, presigned reads (SAS GET), direct PUT begin/complete, server-side copy, DEK migrate/rotate, health, IFileStorageDiagnosticsService listing keys under the container prefix (normalized and traversal-guarded by Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix).
- **Optional compression and two-key encryption.** Same pipeline as Lyo.FileStorage when ICompressionService / ICompressionResolver / ITwoKeyEncryptionService are registered. Reads use metadata CompressionAlgorithm via the resolver.
- **SSE.** Optional encryption scope and customer-provided key (SSE-C via a base64 key on options). Applied to single-blob writes, multipart staging, header range updates, and DEK migrations. See XML docs on AzureBlobFileStorageOptions for presigned/SSE limits.
- **Multipart.** Register AddAzureBlobMultipartUploadService() after AddAzureBlobFileStorageService. Final commit uses SyncCopyFromUriAsync rather than download+re-upload.
- **Resolved suffix cache.** Saved metadata keeps the storage extension/suffix so later reads/copies skip the legacy N+1 "try base then `.gz`/`.lyo.gz`/…" probes. The shared CloudObjectKeyBuilder produces the candidate key directly.
- **Shared traversal guard.** Container/blob prefix normalization and traversal rejection live in Lyo.Exceptions.FileHelpers so the same rules apply across diagnostics listing and core save/direct-upload validation.

## Examples

### DI setup

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
```

### Direct upload via presigned PUT

```csharp
var url = await fileStorage.GetPreSignedReadUrlAsync(fileId, TimeSpan.FromHours(1), pathPrefix, ct);

// Optional Content-Disposition / Content-Type overrides (SAS response headers)
var urlWithHeaders = await fileStorage.GetPreSignedReadUrlAsync(
    fileId, TimeSpan.FromHours(1), pathPrefix,
    new PreSignedReadUrlOptions { ContentDisposition = "attachment", ContentType = "application/pdf" },
    ct);
```

### Sample configuration

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

## Where to read next

| Document | Scope |
| ----------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| [`Lyo.FileStorage/README.md`](../Lyo.FileStorage/README.md) | Core contracts (IFileStorageService), multipart, models, LocalFileStorageService, and FileStorageServiceBaseOptions |
| This file | AzureBlobFileStorageOptions, AzureBlobFileStorageService, AddAzureBlobFileStorageService, SAS, and encryption scope |
| Lyo.FileStorage.S3/README.md | S3-compatible backend (compare multipart and presigned behaviour) |

## AzureBlobFileStorageService

Concrete implementation registered as IFileStorageService (scoped). Mirrors S3FileStorageService for cloud-specific paths (presigned URLs, CopyFileAsync / MoveFileAsync via sync copy then delete source, multipart block uploads). RenameFileAsync is metadata-only (shared base implementation).

## AzureBlobFileStorageOptions

| Property / constant | Typical use |
| ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SectionName (AzureBlobFileStorage) | Default configuration section name |
| LegacyAzureConfigurationSectionName (AzureFileStorageOptions) | Obsolete subsections. AddAzureBlobFileStorageService(string) falls back to BlobFileStorage, then AzureFileStorageOptions, and logs a migration warning. |
| ConnectionString | Connection string for the storage account |
| ContainerName | Container to target |
| BlobPrefix | Optional key prefix within the container |
| EncryptionScope | Server-side encryption scope on Azure |
| CustomerProvidedKeyBase64 | Base64-encoded 256-bit AES key for SSE-C on SDK uploads. Direct/SAS uploads with SSE-C are not supported and fail fast. |
| EnableMetrics | When true, emits metrics if IMetrics is present |
| Inherited: HealthCheckMode, MaxUploadSizeBytes, DefaultAvailability, duplicate/hash options, etc. | See FileStorageServiceBaseOptions xmldoc in core. |

## DI setup

Register Microsoft.Extensions.DependencyInjection and Microsoft.Extensions.Configuration through your host.

> **Keyed / multi-tenant DI.** Lyo.FileStorage.AzureBlob does not currently ship the keyed builder pattern that Lyo.FileStorage.S3 exposes via AddS3FileStorageServiceKeyed. Use AddAzureBlobFileStorageService (non-keyed) per host. If you need multiple Blob backends side-by-side, register each in a separate DI scope or wire them manually via AddKeyedScoped<IFileStorageService>(...) and AddKeyedScoped<AzureBlobFileStorageService>(...) mirroring the constructor arguments of [AzureBlobFileStorageService](AzureBlobFileStorageService.cs). A keyed builder analogous to S3's is on the roadmap.

## Direct upload via presigned PUT

BeginDirectUploadAsync returns a SAS URL plus a RequiredPutHeaders map. Clients must apply those headers verbatim on the PUT. Azure block blob uploads require at minimum `x-ms-blob-type: BlockBlob`, and when the caller supplied a ContentType the map also includes `x-ms-blob-content-type` (so the value is persisted on the blob) and Content-Type for parity with the S3 backend. Customer-Provided-Key (SSE-C) accounts are rejected at begin time. Direct PUT is not currently supported with SSE-C. Presigned reads use the core API:

## Sample configuration

If AzureBlobFileStorage is absent, legacy sections BlobFileStorage and AzureFileStorageOptions still bind.

## Health

IFileStorageService extends IHealth. Call await fileStorage.CheckHealthAsync(ct) directly. HealthCheckMode controls depth.

## Concurrency

AzureBlobFileStorageService follows the core base type: concurrent calls on one instance are supported. IFileMetadataStore, keystores, and ILogger must match the DI lifetimes you chose.

## Tests

Lyo.FileStorage.AzureBlob.Tests provides isolated unit coverage for AzureBlobFileStorageOptions (CPK resolution, section names, base defaults) and the shared CloudObjectKeyBuilder. Path-prefix traversal coverage lives in Lyo.FileStorage.Tests against the shared Lyo.Exceptions.FileHelpers helper. Live container I/O would need Azurite.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `Azure.Storage.Blobs` `12.29.1` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
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