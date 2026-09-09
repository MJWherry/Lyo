using Lyo.Exceptions;
using Lyo.FileStorage.Models;

namespace Lyo.FileStorage.S3;

public sealed class S3FileStorageOptions : FileStorageServiceBaseOptions
{
    public const string SectionName = "S3FileStorageOptions";

    /// <summary>S3 bucket that holds stored objects.</summary>
    public string BucketName { get; set; } = null!;

    /// <summary>Optional S3 bucket region. When omitted, the default AWS region is used.</summary>
    public string? Region { get; set; }

    /// <summary>
    /// Optional AWS access key ID. When set with <see cref="SecretAccessKey" />, used instead of <see cref="Profile" /> or the default credential chain. When both keys are null or
    /// whitespace, <see cref="Profile" /> is used if set; otherwise the default AWS chain (environment, shared credentials file, IAM role).
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>Optional AWS secret access key. When paired with <see cref="AccessKeyId" />, used instead of <see cref="Profile" /> or the default credential chain.</summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Named profile from the shared credentials/config files (<c>~/.aws/credentials</c> and <c>~/.aws/config</c>). Used when static keys are omitted. When unset, the AWS default
    /// chain is used (which looks for the <c>default</c> profile). If set but the profile is missing, client construction fails rather than falling back to <c>default</c>.
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>Optional service URL for S3-compatible hosts (e.g. MinIO, LocalStack). When omitted, standard AWS S3 endpoints are used.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Optional account id for S3-compatible endpoint helpers (e.g. Cloudflare R2; see <see cref="S3FileStorageS3CompatibleExtensions.ApplyCloudflareR2Defaults" />).</summary>
    public string? ProviderAccountId { get; set; }

    /// <summary>Optional prefix applied to every S3 object key (useful for grouping files in a bucket).</summary>
    public string? KeyPrefix { get; set; }

    /// <summary>Optional server-side encryption on new writes (<c>AES256</c>, <c>aws:kms</c>, <c>aws:kms:dsse</c>).</summary>
    public string? ServerSideEncryption { get; set; }

    /// <summary>Optional KMS key id or ARN when <see cref="ServerSideEncryption" /> is a KMS variant.</summary>
    public string? ServerSideEncryptionAwsKmsKeyId { get; set; }

    /// <summary>Throws when BucketName is missing. Access keys stay optional (IAM / profile / default chain).</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(BucketName);
}