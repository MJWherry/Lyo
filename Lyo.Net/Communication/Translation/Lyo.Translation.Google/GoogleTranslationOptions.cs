using System.Diagnostics;
using Lyo.Translation.Models;

namespace Lyo.Translation.Google;

/// <summary>Configuration options for Google Translate service.</summary>
/// <remarks>
/// <para>This class is not thread-safe. Options should be configured during application startup and not modified after service registration.</para>
/// <para>All properties from the base <see cref="TranslationServiceOptions" /> class are also available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GoogleTranslationOptions : TranslationServiceOptions
{
    /// <summary>The default configuration section name for GoogleTranslationOptions.</summary>
    public const string SectionName = "GoogleTranslationOptions";

    /// <summary>Gets or sets the Google Cloud API key.</summary>
    /// <remarks>Keep this secure and never commit it to source control.</remarks>
    public string? ApiKey { get; set; }

    /// <summary>Gets or sets the Google Cloud project ID (required when using service account credentials).</summary>
    public string? ProjectId { get; set; }

    /// <summary>Gets or sets the path to the Google Cloud service account JSON file (optional, for service account authentication).</summary>
    public string? ServiceAccountJsonPath { get; set; }

    /// <summary>Gets or sets the Google Cloud Translate API endpoint (optional, defaults to Google's public endpoint).</summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>Returns a string representation of the options (privacy-safe, does not include credentials).</summary>
    /// <returns>A string containing the ProjectId if available.</returns>
    public override string ToString() => $"ProjectId={ProjectId ?? "NotSet"}";
}