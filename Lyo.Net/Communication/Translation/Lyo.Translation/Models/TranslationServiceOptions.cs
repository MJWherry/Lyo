using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;

namespace Lyo.Translation.Models;

/// <summary>Options for configuring a translation service.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationServiceOptions
{
    /// <summary>Gets or sets the default target language code.</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultTargetLanguage { get; set; }

    /// <summary>Gets or sets the default source language code. If not specified, the service will attempt to detect it.</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? DefaultSourceLanguage { get; set; }

    /// <summary>Gets or sets the maximum text length allowed for translation.</summary>
    public int MaxTextLength { get; set; } = 50000;

    /// <summary>Gets or sets whether to enable metrics tracking.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>Gets or sets the concurrency limit for bulk translation operations.</summary>
    public int BulkTranslationConcurrencyLimit { get; set; } = 10;

    /// <summary>Gets or sets the maximum number of requests allowed in a bulk operation.</summary>
    public int MaxBulkTranslationLimit { get; set; } = 100;

    public override string ToString()
        => $"DefaultTargetLanguage={DefaultTargetLanguage}, DefaultSourceLanguage={DefaultSourceLanguage}, MaxTextLength={MaxTextLength}, EnableMetrics={EnableMetrics}, BulkTranslationConcurrencyLimit={BulkTranslationConcurrencyLimit}, MaxBulkTranslationLimit={MaxBulkTranslationLimit}";
}