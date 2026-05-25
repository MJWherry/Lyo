# Lyo.FileStorage.S3

S3-compatible storage for **Lyo.FileStorage** (AWS S3, **Backblaze B2**, MinIO, etc.) via **AWSSDK.S3**.

## Documentation map

| Document | Scope |
|----------|--------|
| **[`Lyo.FileStorage/README.md`](../Lyo.FileStorage/README.md)** | **`IFileStorageService`** contract, disk backend, **`FileStorageServiceBaseOptions`**, DTOs |
| **`Lyo.FileStorage.Blob/README.md`** | Azure Blob analogue for SAS/direct-upload/copy |
| **This file** | **`S3FileStorageService`**, **`S3FileStorageOptions`**, DI builders (**`AddS3FileStorageServiceKeyed*`**), SSE helpers |

## **`S3FileStorageOptions`** (extends **`FileStorageServiceBaseOptions`**)

| Property | Typical use |
|----------|-------------|
| **`SectionName`** | Default appsettings subsection (`S3FileStorageOptions`) |
| **`BucketName`**, **`Region`** | Target bucket / signing region |
| **`AccessKeyId`**, **`SecretAccessKey`** | Static keys (optional when using IAM/instance profile) |
| **`ServiceUrl`** | S3-compatible API base URL |
| **`ProviderAccountId`** | Compatibility helpers (e.g. Cloudflare R2 account id) |
| **`KeyPrefix`** | Prepended logical folder for every object |
| **`ServerSideEncryption`**, **`ServerSideEncryptionAwsKmsKeyId`** | SSE for streamed saves, multipart, copy, compatible presigned PUT |
| **`EnableMetrics`** | Emit counters/histograms when **`IMetrics`** is registered |
| **Inherited (`FileStorageServiceBaseOptions`)** | Health probing, hashing, duplicates, **`MaxUploadSizeBytes`**, malware-scan gating, etc. |

## Features

- ✅ **S3 API** — same client for AWS and S3-compatible endpoints
- ✅ **Multipart uploads** — keyed `S3MultipartUploadService` is registered with the same key when you call `S3FileStorageServiceBuilder.Build` (unless already registered); if no
  `IMultipartUploadSessionStore` is registered yet, an in-memory store is added (use `AddPostgresFileMetadataStoreKeyed(...).Build()` **before** S3 when using PostgreSQL so
  sessions use the DB). Part size is clamped to the S3 minimum (5 MiB) with an 8 MiB default; total upload limit aligns with `MaxUploadSizeBytes`. Server-side copy is used for the final commit (no download+re-upload round trip).
- ✅ **Streamed PUT spilling** — `S3UploadStream` keeps small payloads in memory and spills to a deletable temp file once it crosses 4 MiB, then uploads via single PUT under 64 MiB or multipart above that, aborting cleanly on any per-part failure
- ✅ **Region Support** - Configurable AWS regions
- ✅ **Custom Endpoints** - Support for S3-compatible services
- ✅ **Key Prefixing** - Organized file storage with key prefixes via the shared `CloudObjectKeyBuilder`
- ✅ **Automatic Path Organization** - Files organized by GUID prefixes (suffix is persisted in metadata so reads skip N+1 probes)
- ✅ **IAM Role Support** - Works with IAM roles for authentication
- ✅ **Diagnostics** — bucket key listing via `IFileStorageDiagnosticsService` (prefix-aware, combines `KeyPrefix`, normalized + traversal-guarded by `Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix`)
- ✅ **Server-side copy & direct PUT** — `CopyFileAsync` (`CopyObject`), `BeginDirectUploadAsync` / `CompleteDirectUploadAsync` (presigned PUT + finalize). `RequiredPutHeaders` is populated when SSE or a signed `Content-Type` applies, courtesy of `S3UploadServerSideEncryption.BuildRequiredPutHeaders`
- ✅ **Presigned GET options** — optional `ContentDisposition` / `ContentType` via `PreSignedReadUrlOptions` (S3 response header overrides). When the caller omits `pathPrefix`, the metadata-stored prefix is used as fallback.

## Configuration

```csharp
using Lyo.FileStorage.S3;
using Lyo.FileStorage.Models;

var options = new S3FileStorageOptions
{
    BucketName = "my-bucket",
    Region = "us-east-1",
    KeyPrefix = "app-files", // Optional global prefix
    AccessKeyId = "your-key", // Optional if using IAM roles
    SecretAccessKey = "your-secret", // Optional if using IAM roles
    // Optional SSE for new uploads, copies, streamed saves, multipart staging, and compatible presigned PUTs:
    ServerSideEncryption = "AES256", // or "aws:kms" / "aws:kms:dsse"
    ServerSideEncryptionAwsKmsKeyId = null // set CMK id/ARN when using KMS
};

var metadataStore = new YourMetadataStore(); // Implement IFileMetadataStore
var service = new S3FileStorageService(options, metadataStore);
```

## S3-Compatible Services

Set **`ServiceUrl`** (and usually **`ForcePathStyle`** is applied automatically when a custom URL is set):

```csharp
var options = new S3FileStorageOptions
{
    BucketName = "my-bucket",
    ServiceUrl = "https://s3-compatible.example.com",
    AccessKeyId = "your-key",
    SecretAccessKey = "your-secret"
};
```

### Backblaze B2

Use **`S3FileStorageBackblazeExtensions.ApplyBackblazeB2Defaults()`** so **`ServiceUrl`** becomes `https://s3.{region}.backblazeb2.com` when **`Region`** is set (e.g.
`us-west-004`). Or call **`AddS3FileStorageServiceKeyedForBackblaze`** to bind the **`BackblazeFileStorage`** section (see *
*`S3FileStorageBackblazeExtensions.BackblazeFileStorageConfigurationSectionName`**) and register the keyed storage builder.

### Other common S3-compatible providers

**`S3FileStorageS3CompatibleExtensions`** provides endpoint URL builders, **`Apply*Defaults`** methods (set **`ServiceUrl`** from **`Region`** / **`ProviderAccountId`** when *
*`ServiceUrl`** is not already set), and **`AddS3FileStorageServiceKeyedFor*`** helpers with default configuration section names.

| Provider                | Region / ids                                                                                          | Endpoint helper                                                                                         | Config section constant                                     |
|-------------------------|-------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|-------------------------------------------------------------|
| **MinIO**               | Set **`ServiceUrl`** to the MinIO server (host or full URL; scheme defaults to `http://` if omitted). | **`GetMinioServiceUrl`**, **`ApplyMinioDefaults`**                                                      | **`MinioFileStorageConfigurationSectionName`**              |
| **Wasabi**              | **`Region`** = Wasabi region (e.g. `us-east-1`)                                                       | **`GetWasabiServiceUrl`**, **`ApplyWasabiDefaults`**                                                    | **`WasabiFileStorageConfigurationSectionName`**             |
| **DigitalOcean Spaces** | **`Region`** = region slug (e.g. `nyc3`)                                                              | **`GetDigitalOceanSpacesServiceUrl`**, **`ApplyDigitalOceanSpacesDefaults`**                            | **`DigitalOceanSpacesFileStorageConfigurationSectionName`** |
| **Cloudflare R2**       | **`ProviderAccountId`** = R2 account id                                                               | **`GetCloudflareR2ServiceUrl`**, **`ApplyCloudflareR2Defaults`** (sets **`Region`** to `auto` if unset) | **`CloudflareR2FileStorageConfigurationSectionName`**       |
| **Scaleway**            | **`Region`** = `fr-par`, `nl-ams`, etc.                                                               | **`GetScalewayObjectStorageServiceUrl`**, **`ApplyScalewayDefaults`**                                   | **`ScalewayFileStorageConfigurationSectionName`**           |
| **Linode**              | **`Region`** = cluster id (e.g. `us-east-1`)                                                          | **`GetLinodeObjectStorageServiceUrl`**, **`ApplyLinodeObjectStorageDefaults`**                          | **`LinodeObjectStorageConfigurationSectionName`**           |

Example (MinIO in code — same builder chain as other keyed S3 storage, e.g. **`UseFileMetadataStore`**, then **`Build(configuration)`**):

```csharp
services.AddS3FileStorageServiceKeyedForMinio("files", o => {
    o.BucketName = "my-bucket";
    o.ServiceUrl = "localhost:9000"; // or https://minio.example.com — scheme optional for host:port
    o.AccessKeyId = "...";
    o.SecretAccessKey = "...";
})
    .UseFileMetadataStore("your-metadata-store-key")
    .Build(configuration);
```

## Production Ready

- ✅ Handles S3-specific errors gracefully
- ✅ Supports IAM role-based authentication
- ✅ Efficient object key lookup
- ✅ Proper resource disposal (**`Dispose()`** and **`IAsyncDisposable.DisposeAsync()`** when the service owns the **`IAmazonS3`** client)
- ✅ Comprehensive error handling
- ✅ Thread-safe operations

## Error Handling

The service handles S3-specific errors:

- **404 Not Found**: Returns null or empty results instead of throwing
- **Access Denied**: Clear error messages for permission issues
- **Network Errors**: Retry logic should be handled at the application level

## File Organization

Files are automatically organized by GUID prefixes:

- Format: `{KeyPrefix}/{guid-prefix-2}/{guid-prefix-2}/{guid}.{extension}`
- Example: `app-files/ab/cd/abcdef1234567890.ag`

## Health Checks

`IFileStorageService` extends `IHealth`. Get health directly from the service: `await fileStorage.CheckHealthAsync()`.

## Tests

`Lyo.FileStorage.S3.Tests` exercises this assembly with isolated, dependency-free unit tests using a `DispatchProxy`-based `IAmazonS3` stub (`Support/FakeAmazonS3`). Covered: `S3UploadServerSideEncryption` header/apply logic, `S3UploadStream` (single PUT + multipart begin→complete + abort + SSE forwarding), `S3GetObjectResponseStream` disposal, the shared `CloudObjectKeyBuilder`, and options invariants. Path-prefix traversal coverage lives in `Lyo.FileStorage.Tests` against the shared `Lyo.Exceptions.FileHelpers` helper. Deeper end-to-end coverage of presigned signing and live bucket I/O would need LocalStack.

## Dependencies

*(Synchronized from `Lyo.FileStorage.S3.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                                 | Version    |
|---------------------------------------------------------|------------|
| `AWSSDK.Core`                                           | `4.0.3.13` |
| `AWSSDK.S3`                                             | `4.0.18.4` |
| `Microsoft.Extensions.Configuration.Abstractions`       | `[10,)`    |
| `Microsoft.Extensions.Configuration.Binder`             | `[10,)`    |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)`    |

### Project references

- [`Lyo.Common`](../../../Core/Common/Lyo.Common/README.md)
- [`Lyo.Compression`](../../Compression/Lyo.Compression/README.md)
- [`Lyo.Encryption`](../../../Security/Encryption/Lyo.Encryption/README.md)
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.FileMetadataStore`](../../FileMetadataStore/Lyo.FileMetadataStore/README.md)
- [`Lyo.FileStorage`](../Lyo.FileStorage/README.md)