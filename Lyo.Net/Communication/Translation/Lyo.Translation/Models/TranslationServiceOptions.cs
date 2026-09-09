using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;

namespace Lyo.Translation.Models;

/// <summary>Settings that configure a translation service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationServiceOptions
{
    /// <summary>Target language used when a request does not specify one.</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultTargetLanguage { get; set; }

    /// <summary>Source language used when a request omits one. When unset the service tries to detect it.</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultSourceLanguage { get; set; }

    /// <summary>Longest text length accepted for a single translation.</summary>
    public int MaxTextLength { get; set; } = 50000;

    /// <summary>Whether metrics are recorded for translation work.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>How many bulk translation items may run at once.</summary>
    public int BulkTranslationConcurrencyLimit { get; set; } = 10;

    /// <summary>Upper bound on how many requests a bulk call may contain.</summary>
    public int MaxBulkTranslationLimit { get; set; } = 100;

    /// <inheritdoc />
    public override string ToString()
        => $"DefaultTargetLanguage={DefaultTargetLanguage}, DefaultSourceLanguage={DefaultSourceLanguage}, MaxTextLength={MaxTextLength}, EnableMetrics={EnableMetrics}, BulkTranslationConcurrencyLimit={BulkTranslationConcurrencyLimit}, MaxBulkTranslationLimit={MaxBulkTranslationLimit}";
}