using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;

namespace Lyo.Translation.Models;

/// <summary>One translation job: source text plus language choices.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class TranslationRequest
{
    /// <summary>Source text that should be translated.</summary>
    public string? Text { get; set; }

    /// <summary>Language the text should be translated into.</summary>
    [JsonConverter(typeof(LanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo TargetLanguageCode { get; set; } = LanguageCodeInfo.Unknown;

    /// <summary>Language of the source text. When omitted the service tries to detect it.</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? SourceLanguage { get; set; }

    /// <summary>Empty request; assign <see cref="Text" /> before calling translate.</summary>
    public TranslationRequest() { }

    /// <summary>Builds a request from text and a target language.</summary>
    /// <param name="text">Content to translate.</param>
    /// <param name="targetLanguageCode">Language for the output.</param>
    /// <param name="sourceLanguage">Optional source language; leaving it unset usually enables auto-detection.</param>
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