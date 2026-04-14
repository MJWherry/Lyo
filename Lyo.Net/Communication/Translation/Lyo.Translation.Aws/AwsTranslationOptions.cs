using System.Diagnostics;
using Lyo.Translation.Models;

namespace Lyo.Translation.Aws;

/// <summary>Configuration options for AWS Translate service.</summary>
/// <remarks>
/// <para>This class is not thread-safe. Options should be configured during application startup and not modified after service registration.</para>
/// <para>All properties from the base <see cref="TranslationServiceOptions" /> class are also available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AwsTranslationOptions : TranslationServiceOptions
{
    /// <summary>The default configuration section name for AwsTranslationOptions.</summary>
    public const string SectionName = "AwsTranslationOptions";

    /// <summary>Gets or sets the AWS access key ID (required if not using IAM roles).</summary>
    /// <remarks>Keep this secure and never commit it to source control. Consider using IAM roles instead.</remarks>
    public string? AccessKeyId { get; set; }

    /// <summary>Gets or sets the AWS secret access key (required if not using IAM roles).</summary>
    /// <remarks>Keep this secure and never commit it to source control. Consider using IAM roles instead.</remarks>
    public string? SecretAccessKey { get; set; }

    /// <summary>Gets or sets the AWS region (e.g., "us-east-1", "eu-west-1").</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Gets or sets the AWS service URL (optional, for local testing or custom endpoints).</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Returns a string representation of the options (privacy-safe, does not include credentials).</summary>
    /// <returns>A string containing the Region.</returns>
    public override string ToString() => $"Region={Region}";
}