using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;

namespace Lyo.Translation.Models;

/// <summary>Represents a translation request with text and language options.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationRequest
{
    /// <summary>Gets or sets the text to translate.</summary>
    public string? Text { get; set; }

    /// <summary>Gets or sets the target language code.</summary>
    [JsonConverter(typeof(LanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo TargetLanguageCode { get; set; } = LanguageCodeInfo.Unknown;

    /// <summary>Gets or sets the source language code. If not specified, the service will attempt to detect it.</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? SourceLanguage { get; set; }

    /// <summary>Initializes an empty request (set <see cref="Text" /> before translating).</summary>
    public TranslationRequest() { }

    /// <summary>Initializes a request with text and target language.</summary>
    /// <param name="text">Source content.</param>
    /// <param name="targetLanguageCode">Desired output language.</param>
    /// <param name="sourceLanguage">Optional source language; omission usually triggers auto-detection.</param>
    public TranslationRequest(string text, LanguageCodeInfo targetLanguageCode, LanguageCodeInfo? sourceLanguage = null)
    {
        Text = text;
        TargetLanguageCode = targetLanguageCode;
        SourceLanguage = sourceLanguage;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string> {
            $"Text: {Text?.Substring(0, Math.Min(Text?.Length ?? 0, 50))}{(Text?.Length > 50 ? "..." : "")}",
            $"TargetLanguage: {TargetLanguageCode.Iso6393 ?? TargetLanguageCode.Iso6391 ?? TargetLanguageCode.Bcp47}"
        };

        if (SourceLanguage != null)
            parts.Add($"SourceLanguage: {SourceLanguage.Iso6393 ?? SourceLanguage.Iso6391 ?? SourceLanguage.Bcp47}");

        return string.Join(" | ", parts);
    }
}