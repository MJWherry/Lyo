using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Translation.Models;

namespace Lyo.Translation.Aws;

/// <summary>Settings for the AWS Translate provider.</summary>
/// <remarks>
/// <para>Not thread-safe. Configure during startup and leave the instance alone after it is registered.</para>
/// <para>Inherited members from <see cref="TranslationServiceOptions" /> remain available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AwsTranslationOptions : TranslationServiceOptions
{
    /// <summary>Default config section used for AwsTranslationOptions.</summary>
    public const string SectionName = "AwsTranslationOptions";

    /// <summary>AWS access key ID (needed unless IAM roles supply credentials).</summary>
    /// <remarks>Treat as secret; do not commit it. Prefer IAM roles when possible.</remarks>
    public string? AccessKeyId { get; set; }

    /// <summary>AWS secret access key (needed unless IAM roles supply credentials).</summary>
    /// <remarks>Treat as secret; do not commit it. Prefer IAM roles when possible.</remarks>
    public string? SecretAccessKey { get; set; }

    /// <summary>AWS region, such as "us-east-1" or "eu-west-1".</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Optional AWS service URL for local tests or a custom endpoint.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Privacy-safe string form of the options (credentials omitted).</summary>
    /// <returns>A string that includes the Region.</returns>
    public override string ToString() => $"Region={Region}";

    /// <summary>Throws when Region is missing.</summary>
    public void Validate() => ArgumentHelpers.ThrowIfNullOrWhiteSpace(Region);
}