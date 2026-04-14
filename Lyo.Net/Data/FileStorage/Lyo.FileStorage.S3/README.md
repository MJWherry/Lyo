# Lyo.FileStorage.S3

S3-compatible storage for **Lyo.FileStorage** (AWS S3, **Backblaze B2**, MinIO, etc.) via **AWSSDK.S3**.

## Features

- ✅ **S3 API** — same client for AWS and S3-compatible endpoints
- ✅ **Multipart uploads** — keyed `S3MultipartUploadService` is registered with the same key when you call `S3FileStorageServiceBuilder.Build` (unless already registered); if no
  `IMultipartUploadSessionStore` is registered yet, an in-memory store is added (use `AddPostgresFileMetadataStoreKeyed(...).Build()` **before** S3 when using PostgreSQL so
  sessions use the DB)
- ✅ **Region Support** - Configurable AWS regions
- ✅ **Custom Endpoints** - Support for S3-compatible services
- ✅ **Key Prefixing** - Organized file storage with key prefixes
- ✅ **Automatic Path Organization** - Files organized by GUID prefixes
- ✅ **IAM Role Support** - Works with IAM roles for authentication

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
    SecretAccessKey = "your-secret" // Optional if using IAM roles
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
- ✅ Proper resource disposal
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




<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.FileStorage.S3.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `AWSSDK.Core` | `4.0.3.13` |
| `AWSSDK.S3` | `4.0.18.4` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10,)` |

### Project references

- `Lyo.Common`
- `Lyo.Compression`
- `Lyo.Encryption`
- `Lyo.Exceptions`
- `Lyo.FileMetadataStore`
- `Lyo.FileStorage`

## Public API (generated)

Top-level `public` types in `*.cs` (*9*). Nested types and file-scoped namespaces may omit some entries.

- `Constants`
- `Extensions`
- `Metrics`
- `S3FileStorageBackblazeExtensions`
- `S3FileStorageOptions`
- `S3FileStorageS3CompatibleExtensions`
- `S3FileStorageService`
- `S3FileStorageServiceBuilder`
- `S3MultipartUploadService`

<!-- LYO_README_SYNC:END -->

## License

[Your License Here]

