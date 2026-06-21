# Lyo.FileStorage.Blob

**Azure Blob Storage** implementation of [`IFileStorageService`](../Lyo.FileStorage/README.md) using **`Azure.Storage.Blobs`**. The package id and namespace *
*`Lyo.FileStorage.Blob`** reflect the blob abstraction; configuration and types use **`Blob*`** naming (with a legacy **`AzureFileStorageOptions`** appsettings subsection for
migration).

Architecture, duplicate handling, and threat-model context for storage live in **`Lyo.FileStorage`**; this README is the assembly surface plus Azure-specific options.

## Documentation map

| Document                                                        | Scope                                                                                                                             |
|-----------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| **[`Lyo.FileStorage/README.md`](../Lyo.FileStorage/README.md)** | Core contracts (**`IFileStorageService`**), multipart, models, **`LocalFileStorageService`**, **`FileStorageServiceBaseOptions`** |
| **This file**                                                   | **`BlobFileStorageOptions`**, **`BlobFileStorageService`**, **`AddBlobFileStorageService`**, SAS / encryption scope               |
| **`Lyo.FileStorage.S3/README.md`**                              | S3-compatible backend (comparison for multipart and presigned behaviour)                                                          |

## Features

- **Same contract as Local / S3** — Save, stream save, multipart, presigned reads (SAS GET), direct PUT begin/complete, server-side copy, DEK migrate/rotate, health, *
  *`IFileStorageDiagnosticsService`** listing keys under container prefix (normalized + traversal-guarded by **`Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix`**)
- **Optional compression & two-key encryption** — Same pipeline as **`Lyo.FileStorage`** when **`ICompressionService`** / **`ICompressionResolver`** / **`ITwoKeyEncryptionService`** are registered (reads use metadata **`CompressionAlgorithm`** via the resolver)
- **SSE** — Optional **encryption scope** and **customer-provided key** (SSE-C via base64 key on options); applied to single-blob writes, multipart staging, **header range updates
  **, and DEK migrations. See XML docs on **`BlobFileStorageOptions`** for presigned/SSE limits.
- **Multipart** — Register **`AddBlobMultipartUploadService()`** after **`AddBlobFileStorageService`**. Final commit uses **`SyncCopyFromUriAsync`** rather than download+re-upload.
- **Staged upload** — Register **`AddBlobStagedFileUploadService()`** after blob storage. Issues SAS PUT URLs under `.stage/{stageId}/object` with required `x-ms-blob-type: BlockBlob` (and optional encryption scope headers). **Not supported** when **`CustomerProvidedKeyBase64`** (SSE-C) is configured — fail fast at **`BeginAsync`**.
- **Resolved suffix cache** — Saved metadata persists the storage extension/suffix so subsequent reads/copies skip the legacy N+1 "try base then `.gz`/`.lyo.gz`/…" probes (the
  shared `CloudObjectKeyBuilder` produces the candidate key directly).
- **Shared traversal guard** — Container/blob prefix normalization and traversal rejection live in `Lyo.Exceptions.FileHelpers` so the same rules apply across diagnostics listing
  and core save/direct-upload validation.

## **`BlobFileStorageService`**

Concrete implementation registered as **`IFileStorageService`** (scoped). Mirrors **`S3FileStorageService`** for cloud-specific paths (presigned URLs, **`CopyBlobAsync`**-style
behaviour, multipart block uploads).

## Options — **`BlobFileStorageOptions`** extends **`FileStorageServiceBaseOptions`**

| Property / constant                                                                                                            | Typical use                                                                                                          |
|--------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| **`SectionName`** (`BlobFileStorage`)                                                                                          | Default configuration section                                                                                        |
| **`LegacyAzureConfigurationSectionName`** (`AzureFileStorageOptions`)                                                          | Obsolete subsection; **`AddBlobFileStorageService(string)`** falls back here with a migration warning                |
| **`ConnectionString`**                                                                                                         | Storage account connection string                                                                                    |
| **`ContainerName`**                                                                                                            | Target container                                                                                                     |
| **`BlobPrefix`**                                                                                                               | Optional key prefix inside the container                                                                             |
| **`EncryptionScope`**                                                                                                          | Azure **server-side** encryption scope                                                                               |
| **`CustomerProvidedKeyBase64`**                                                                                                | Base64-encoded 256-bit AES key for SSE-C on SDK uploads; direct/SAS uploads with SSE-C are not supported (fail fast) |
| **`EnableMetrics`**                                                                                                            | When true, emits metrics if **`IMetrics`** is available                                                              |
| **Inherited:** **`HealthCheckMode`**, **`MaxUploadSizeBytes`**, **`RequireScanBeforeAvailable`**, duplicate/hash options, etc. | See **`FileStorageServiceBaseOptions`** xmldoc in core                                                               |

## Dependency injection (**`Extensions`** — namespace **`Lyo.FileStorage.Blob`**)

Register **`Microsoft.Extensions.DependencyInjection`** and **`Microsoft.Extensions.Configuration`** (via your host).

```csharp
using Lyo.FileStorage.Blob;
using Lyo.FileStorage.Models;

// Options instance
services.AddBlobFileStorageService(new BlobFileStorageOptions
{
    ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...",
    ContainerName = "my-container",
    BlobPrefix = "files"
});

// Or bind from configuration (prefers BlobFileStorage, then legacy AzureFileStorageOptions section)
services.AddBlobFileStorageService(BlobFileStorageOptions.SectionName);

// Multipart uploads (uses in-memory session store unless you replace it)
services.AddBlobMultipartUploadService();

// Staged uploads (uses in-memory store unless Postgres/Sqlite metadata builder registered first)
services.AddBlobStagedFileUploadService();
```

> **Keyed / multi-tenant DI** — `Lyo.FileStorage.Blob` does not currently ship the keyed builder pattern that `Lyo.FileStorage.S3` exposes via `AddS3FileStorageServiceKeyed`. Use *
*`AddBlobFileStorageService`** (non-keyed) per host; if you need multiple Blob backends side-by-side, register each in a separate DI scope or wire them manually via
`AddKeyedScoped<IFileStorageService>(...)` and `AddKeyedScoped<BlobFileStorageService>(...)` mirroring the constructor arguments of [
`BlobFileStorageService`](BlobFileStorageService.cs). A keyed builder analogous to S3's is on the roadmap.

### Direct upload (presigned PUT)

`BeginDirectUploadAsync` returns a SAS URL plus a `RequiredPutHeaders` map. Clients **must** apply those headers verbatim on the PUT — Azure block blob uploads require at minimum
`x-ms-blob-type: BlockBlob`, and when the caller supplied a `ContentType` the map also includes `x-ms-blob-content-type` (so the value is persisted on the blob) and `Content-Type`
for parity with the S3 backend. Customer-Provided-Key (SSE-C) accounts are rejected at begin time — direct PUT is not currently supported with SSE-C.

Presigned reads use the core API:

```csharp
var url = await fileStorage.GetPreSignedReadUrlAsync(fileId, TimeSpan.FromHours(1), pathPrefix, ct);

// Optional Content-Disposition / Content-Type overrides (SAS response headers)
var urlWithHeaders = await fileStorage.GetPreSignedReadUrlAsync(
    fileId, TimeSpan.FromHours(1), pathPrefix,
    new PreSignedReadUrlOptions { ContentDisposition = "attachment", ContentType = "application/pdf" },
    ct);
```

## Configuration example

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

Legacy subsection name **`AzureFileStorageOptions`** still binds if **`BlobFileStorage`** is absent.

## Health

**`IFileStorageService`** extends **`IHealth`**. Call **`await fileStorage.CheckHealthAsync(ct)`** directly. Depth is controlled by **`HealthCheckMode`**.

## Thread safety

**`BlobFileStorageService`** follows the core base type: concurrent calls on one instance should be OK; **`IFileMetadataStore`**, keystores, and **`ILogger`** must be consistent
with your DI lifetimes.

## Tests

`Lyo.FileStorage.Blob.Tests` provides isolated unit coverage for `BlobFileStorageOptions` (CPK resolution, section names, base defaults), **`BlobStagedFileUploadService`** (offline SAS PUT generation), and the shared `CloudObjectKeyBuilder`.
Path-prefix traversal coverage lives in `Lyo.FileStorage.Tests` against the shared `Lyo.Exceptions.FileHelpers` helper. Live container I/O would need Azurite.

## Dependencies

*(From `Lyo.FileStorage.Blob.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package               | Version    |
|-----------------------|------------|
| `Azure.Storage.Blobs` | `[12.27,)` |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Compression`](../../Compression/Lyo.Compression/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.FileMetadataStore`](../../FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage`](../Lyo.FileStorage/README.md)
