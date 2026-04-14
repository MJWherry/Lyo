using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.S3;

public sealed class S3FileStorageOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "S3FileStorageOptions";

    /// <summary> The name of the S3 bucket where files will be stored. </summary>
    public string BucketName { get; set; } = null!;

    /// <summary> Optional region for the S3 bucket. If not specified, uses the default AWS region. </summary>
    public string? Region { get; set; }

    /// <summary> Optional AWS access key ID. If not specified, uses default credential chain. </summary>
    public string? AccessKeyId { get; set; }

    /// <summary> Optional AWS secret access key. If not specified, uses default credential chain. </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>Optional service URL for S3-compatible services (e.g., MinIO, LocalStack). If not specified, uses standard AWS S3 endpoints.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Optional account id for S3-compatible endpoint helpers (e.g. Cloudflare R2 — see <see cref="S3FileStorageS3CompatibleExtensions.ApplyCloudflareR2Defaults" />).</summary>
    public string? ProviderAccountId { get; set; }

    /// <summary> Optional prefix to use for all S3 object keys. Useful for organizing files in a bucket. </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>Enable metrics collection for file storage operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;
}