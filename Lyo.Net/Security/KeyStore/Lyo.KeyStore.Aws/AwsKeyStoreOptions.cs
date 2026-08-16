namespace Lyo.KeyStore.Aws;

/// <summary>Configuration for AWS credentials and settings.</summary>
public class AwsKeyStoreOptions
{
    public const string SectionName = "AwsKeyStore";

    /// <summary>AWS Access Key ID. When set with <see cref="SecretAccessKey" />, used instead of <see cref="Profile" /> or the default credential chain.</summary>
    public string? AccessKeyId { get; set; }

    /// <summary>AWS Secret Access Key. When set with <see cref="AccessKeyId" />, used instead of <see cref="Profile" /> or the default credential chain.</summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Named profile from the shared credentials/config files (<c>~/.aws/credentials</c> and <c>~/.aws/config</c>). Used when static keys are omitted. When unset, the AWS default
    /// credential chain is used (which looks for the <c>default</c> profile). If set but the profile is missing, registration fails rather than falling back to <c>default</c>.
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>AWS Region (e.g., "us-east-2").</summary>
    public string? Region { get; set; }

    /// <summary>AWS Secrets Manager secret name prefix (e.g., "dev/CourtCanary/FileStore").</summary>
    public string? SecretNamePrefix { get; set; }
}