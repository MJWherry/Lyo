# Lyo.FileStorage.S3

Lyo.FileStorage on S3-compatible endpoints (AWS S3, Backblaze B2, MinIO, and others) through AWSSDK.S3.

## Features

- **S3 API.** One client for AWS and S3-compatible endpoints.
- **Multipart uploads.** Keyed S3MultipartUploadService is registered with the same key when you call S3FileStorageServiceBuilder.Build (unless already registered). If no IMultipartUploadSessionStore is registered yet, an in-memory store is added (use AddPostgresFileMetadataStoreKeyed(...).Build() before S3 when using PostgreSQL so sessions use the DB). Part size is clamped to the S3 minimum (5 MiB) with an 8 MiB default. Total upload limit aligns with MaxUploadSizeBytes. Server-side copy is used for the final commit (no download+re-upload round trip).
- **Streamed PUT spilling.** S3UploadStream keeps small payloads in memory and spills to a deletable temp file once it crosses 4 MiB, then uploads via a single PUT under 64 MiB or multipart above that, aborting cleanly on any per-part failure.
- **Region.** AWS regions are configurable.
- **Custom endpoints.** Point ServiceUrl at S3-compatible services.
- **Key prefixing.** Organize objects with key prefixes via the shared CloudObjectKeyBuilder.
- **Path organization.** Files are organized by GUID prefixes. The suffix is persisted in metadata so reads skip N+1 probes.
- **IAM roles.** Authentication works with IAM roles.
- **Diagnostics.** Bucket key listing via IFileStorageDiagnosticsService (prefix-aware; combines KeyPrefix, normalized and traversal-guarded by Lyo.Exceptions.FileHelpers.NormalizeAndValidatePathPrefix).
- **Server-side copy, move, and direct PUT.** CopyFileAsync (CopyObject), MoveFileAsync (CopyObject then delete source, same file id), BeginDirectUploadAsync / CompleteDirectUploadAsync (presigned PUT + finalize). RequiredPutHeaders is populated when SSE or a signed Content-Type applies, via S3UploadServerSideEncryption.BuildRequiredPutHeaders. RenameFileAsync updates display metadata only.
- **Presigned GET options.** Optional ContentDisposition / ContentType via PreSignedReadUrlOptions (S3 response header overrides). When the caller omits pathPrefix, the metadata-stored prefix is used as the fallback.

## Examples

### Configuration

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

### Keyed S3FileStorageServiceBuilder registration

```csharp
services
    .AddS3FileStorageServiceKeyed("client-files")
    .UseFileMetadataStore("postgres-filemetadatastore")
    .UseEncryptionService("two-key-aws")
    .ConfigureS3FileStorage("S3FileStorageOptions")
    .Build(configuration);
```

## Where to read next

| Document | Scope |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| [`Lyo.FileStorage/README.md`](../Lyo.FileStorage/README.md) | IFileStorageService contract, disk backend, FileStorageServiceBaseOptions, DTOs |
| Lyo.FileStorage.AzureBlob/README.md | Azure Blob counterpart for SAS/direct-upload/copy |
| This file | S3FileStorageService, S3FileStorageOptions, DI builders (AddS3FileStorageServiceKeyed*), and SSE helpers |

Compression and encryption follow FileStorageServiceBase: optional ICompressionResolver (metadata-driven decompress on read; see [Lyo.FileStorage, Compression resolver](../Lyo.FileStorage/README.md#compression-resolver)) and ITwoKeyEncryptionService.

## S3FileStorageOptions

| Property | Typical use |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SectionName | Default appsettings subsection name (S3FileStorageOptions) |
| BucketName, Region | Target bucket and signing region |
| AccessKeyId, SecretAccessKey | Static keys (optional). When both are set they win over Profile. Omit them or leave them empty/whitespace to use Profile or the machine default credential chain (env / shared credentials / IAM). |
| Profile | Named AWS profile from ~/.aws/credentials / ~/.aws/config. Used when static keys are omitted. If set but missing, client construction fails rather than falling back to the default. |
| ServiceUrl | Base URL for an S3-compatible API |
| ProviderAccountId | Compatibility helpers (for example Cloudflare R2 account id) |
| KeyPrefix | Logical folder prepended to every object |
| ServerSideEncryption, ServerSideEncryptionAwsKmsKeyId | SSE for streamed saves, multipart, copy, and compatible presigned PUT |
| EnableMetrics | Emit counters/histograms when IMetrics is registered |
| Inherited (FileStorageServiceBaseOptions) | Health probing, hashing, duplicates, MaxUploadSizeBytes, DefaultAvailability, and related flags. |

## S3-compatible endpoints

Set ServiceUrl (and ForcePathStyle is usually applied automatically when a custom URL is set):

```csharp
var options = new S3FileStorageOptions
{
    BucketName = "my-bucket",
    ServiceUrl = "https://s3-compatible.example.com",
    AccessKeyId = "your-key",
    SecretAccessKey = "your-secret"
};
```

## Backblaze B2

Use S3FileStorageBackblazeExtensions.ApplyBackblazeB2Defaults() so ServiceUrl becomes `https://s3.{region}.backblazeb2.com` when Region is set (e.g. `us-west-004`). Or call AddS3FileStorageServiceKeyedForBackblaze to bind the BackblazeFileStorage section (see S3FileStorageBackblazeExtensions.BackblazeFileStorageConfigurationSectionName) and register the keyed storage builder.

## Other S3-compatible providers

S3FileStorageS3CompatibleExtensions provides endpoint URL builders, Apply*Defaults methods (set ServiceUrl from Region / ProviderAccountId when ServiceUrl is not already set), and AddS3FileStorageServiceKeyedFor* helpers with default configuration section names.

| Provider | Region / ids | Endpoint helper | Config section constant |
| ------------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| MinIO | Set ServiceUrl to the MinIO server (host or full URL; scheme defaults to http:// if omitted). | GetMinioServiceUrl, ApplyMinioDefaults | MinioFileStorageConfigurationSectionName |
| Wasabi | Region = Wasabi region (e.g. us-east-1) | GetWasabiServiceUrl, ApplyWasabiDefaults | WasabiFileStorageConfigurationSectionName |
| DigitalOcean Spaces | Region = region slug (e.g. nyc3) | GetDigitalOceanSpacesServiceUrl, ApplyDigitalOceanSpacesDefaults | DigitalOceanSpacesFileStorageConfigurationSectionName |
| Cloudflare R2 | ProviderAccountId = R2 account id | GetCloudflareR2ServiceUrl, ApplyCloudflareR2Defaults (sets Region to auto when unset) | CloudflareR2FileStorageConfigurationSectionName |
| Scaleway | Region = fr-par, nl-ams, etc. | GetScalewayObjectStorageServiceUrl, ApplyScalewayDefaults | ScalewayFileStorageConfigurationSectionName |
| Linode | Region = cluster id (e.g. us-east-1) | GetLinodeObjectStorageServiceUrl, ApplyLinodeObjectStorageDefaults | LinodeObjectStorageConfigurationSectionName |

Example (MinIO in code, same builder chain as other keyed S3 storage, e.g. UseFileMetadataStore, then Build(configuration)):

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

## S3FileStorageServiceBuilder

AddS3FileStorageServiceKeyed(string keyName) returns a fluent builder that owns the keyed S3FileStorageService + IFileStorageService registration plus any auxiliary services it touches:

| Method | Purpose |
| ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `UseFileMetadataStore(keyName)` | Reuse an already-registered keyed `IFileMetadataStore` (for example from `AddPostgresFileMetadataStoreKeyed(...)`). |
| `ConfigureFileMetadataStore(configSectionName)` | Reserved (throws today; register the metadata store separately and pass its key). |
| `ConfigureFileMetadataStore(Func<IServiceProvider, IFileMetadataStore>)` | Inline metadata-store factory. |
| `UseEncryptionService(keyName)` | Reuse a keyed `ITwoKeyEncryptionService`. |
| `ConfigureEncryptionService(Func<IServiceProvider, ITwoKeyEncryptionService>)` | Inline encryption-service factory (registered as a keyed singleton under the file-storage key). |
| `ConfigureS3FileStorage(string configSectionName = S3FileStorageOptions.SectionName)` | Bind `S3FileStorageOptions` from configuration as a singleton. |
| `ConfigureS3FileStorage(Action<S3FileStorageOptions>)` | Configure options inline. |
| `UseKeyStore(keyName)` / `ConfigureKeyStore(configSectionName)` | Reference an existing key store. Actual key-store registration is done by `Lyo.KeyStore` extensions. |
| `Build(IConfiguration configuration)` | Finalizes registration: registers `IAmazonS3` (via `AddAmazonS3FromConfiguration`), an `IMultipartUploadSessionStore` (in-memory fallback), and keyed `S3MultipartUploadService` when those are not already registered. |

## Other DI entry points

| Extension | Purpose |
| ---------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `services.AddAmazonS3FromConfiguration(configuration, configSectionName = S3FileStorageOptions.SectionName)` | Standalone `IAmazonS3` registration (also called automatically by the builder). Honours `AccessKeyId`/`SecretAccessKey` when both are non-whitespace; otherwise `Profile` when set; otherwise the default credential chain. Also honours `Region`, `ServiceUrl` (forces path-style addressing when set). |
| `services.AddKeyedS3MultipartUploadService(string serviceKey)` | Registers the keyed multipart service alone (for example when replacing the default registration created by `Build`). |
| `services.AddKeyedAwsMultipartUploadService(string serviceKey)` | Alias for `AddKeyedS3MultipartUploadService`, named for callers thinking in AWS SDK terms. |
| `S3FileStorageBackblazeExtensions.ApplyBackblazeB2Defaults()` and `S3FileStorageS3CompatibleExtensions.Apply*Defaults` | See the provider matrix below. They only set `ServiceUrl`/`Region` defaults when those fields are unset. |

## `S3UploadServerSideEncryption`

Lyo.FileStorage.S3.S3UploadServerSideEncryption is the shared helper that translates ServerSideEncryption + ServerSideEncryptionAwsKmsKeyId into the right AWS SDK enum, applies headers to PutObjectRequest / multipart InitiateMultipartUploadRequest, and emits RequiredPutHeaders on DirectUploadBeginResult so a browser PUT to the presigned URL carries the same SSE/Content-Type values that were used to sign the URL. Supported values:

| `ServerSideEncryption` | Effect |
| ------------------------ | ----------------------------------------------------------------------------- |
| `null` / `""` / `"None"` | No SSE applied. |
| `"AES256"` | SSE-S3 (server-managed keys). |
| `"aws:kms"` | SSE-KMS using the optional `ServerSideEncryptionAwsKmsKeyId` (CMK id or ARN). |
| `"aws:kms:dsse"` | SSE-KMS with dual-layer encryption (DSSE). |

## Notes

- IAM role-based authentication is supported.
- Dispose() and IAsyncDisposable.DisposeAsync() run when this service owns the IAmazonS3 client.

## Error handling

- **404 Not Found.** Returns null or empty results rather than throwing.
- **Access Denied.** Permission issues surface clear error messages.
- **Network Errors.** Retry logic belongs at the application level.

## Object key layout

- Format: `{KeyPrefix}/{guid-prefix-2}/{guid-prefix-2}/{guid}.{extension}`
- Example: `app-files/ab/cd/abcdef1234567890.ag`

## Health checks

`IFileStorageService` extends `IHealth`. Read health from the service: `await fileStorage.CheckHealthAsync()`.

## Tests

`Lyo.FileStorage.S3.Tests` exercises this assembly with isolated, dependency-free unit tests using a `DispatchProxy`-based `IAmazonS3` stub (`Support/FakeAmazonS3`). Covered: `S3UploadServerSideEncryption` header/apply logic, `S3UploadStream` (single PUT + multipart begin→complete + abort + SSE forwarding), `S3GetObjectResponseStream` disposal, the shared `CloudObjectKeyBuilder`, and options invariants. Path-prefix traversal coverage lives in `Lyo.FileStorage.Tests` against the shared `Lyo.Exceptions.FileHelpers` helper. Deeper end-to-end coverage of presigned signing and live bucket I/O would need LocalStack.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.Compression` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.FileMetadataStore` (direct, lyo)
- `Lyo.FileStorage` (direct, lyo)
- `AWSSDK.Core` `4.0.100.4` (direct, third-party)
- `AWSSDK.S3` `4.0.101` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
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
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.DataAnnotations` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)