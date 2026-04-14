namespace Lyo.Keystore.Aws;

/// <summary>Configuration for AWS credentials and settings.</summary>
public class AwsKeystoreOptions
{
    public const string SectionName = "AwsKeyStore";

    /// <summary>AWS Access Key ID.</summary>
    public string? AccessKeyId { get; set; }

    /// <summary>AWS Secret Access Key.</summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>AWS Region (e.g., "us-east-2").</summary>
    public string? Region { get; set; }

    /// <summary>AWS Secrets Manager secret name prefix (e.g., "dev/CourtCanary/FileStore").</summary>
    public string? SecretNamePrefix { get; set; }
}