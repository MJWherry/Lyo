using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Translation.Models;

namespace Lyo.Translation.Google;

/// <summary>Settings for the Google Translate provider.</summary>
/// <remarks>
/// <para>Not thread-safe. Configure during startup and leave the instance alone after it is registered.</para>
/// <para>Inherited members from <see cref="TranslationServiceOptions" /> remain available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GoogleTranslationOptions : TranslationServiceOptions
{
    /// <summary>Default config section used for GoogleTranslationOptions.</summary>
    public const string SectionName = "GoogleTranslationOptions";

    /// <summary>Google Cloud API key.</summary>
    /// <remarks>Treat as secret; do not commit it.</remarks>
    public string? ApiKey { get; set; }

    /// <summary>Google Cloud project ID (needed when authenticating with a service account).</summary>
    public string? ProjectId { get; set; }

    /// <summary>Optional path to a Google Cloud service-account JSON file.</summary>
    public string? ServiceAccountJsonPath { get; set; }

    /// <summary>Google Cloud Translate API endpoint; defaults to Google's public URL when unset.</summary>
    public string ApiEndpoint { get; set; } = "https://translation.googleapis.com/language/translate/v2";

    /// <summary>Privacy-safe string form of the options (credentials omitted).</summary>
    /// <returns>A string that includes ProjectId when it is set.</returns>
    public override string ToString() => $"ProjectId={ProjectId ?? "NotSet"}";

    /// <summary>Throws when neither an API key nor a project/service-account pair is configured.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(ApiEndpoint);
        var hasKey = !string.IsNullOrWhiteSpace(ApiKey);
        var hasAccount = !string.IsNullOrWhiteSpace(ProjectId) || !string.IsNullOrWhiteSpace(ServiceAccountJsonPath);
        ArgumentHelpers.ThrowIf(!hasKey && !hasAccount, "Set ApiKey or ProjectId/ServiceAccountJsonPath.", nameof(ApiKey));
    }
}