using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;
using Lyo.Tts.Models;
#if NETSTANDARD2_0
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.Tts.AwsPolly;

/// <summary>Represents a Text-to-Speech request with text, voice, and output format options.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class AwsPollyTtsRequest : TtsRequest
{
    /// <summary>Gets or sets the voice identifier to use for synthesis.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AwsPollyVoiceId? VoiceId {
        get => string.IsNullOrWhiteSpace(VoiceIdInternal) ? null : Enum.TryParse<AwsPollyVoiceId>(VoiceIdInternal, true, out var v) ? v : null;
        set => VoiceIdInternal = value?.ToString();
    }

    /// <summary>Gets or sets the language code (BCP 47 format, e.g., "en-US", "en-GB").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? LanguageCode {
        get {
            if (string.IsNullOrWhiteSpace(LanguageInternal))
                return null;

            var lang = LanguageCodeInfo.FromBcp47(LanguageInternal);
            return lang == LanguageCodeInfo.Unknown ? null : lang;
        }
        set => LanguageInternal = value?.Bcp47;
    }

    /// <summary>Gets or sets the output format (e.g., "mp3", "wav", "ogg").</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AudioFormat OutputFormat {
        get => ParseAudioFormat(AudioFormatInternal) ?? AudioFormat.Mp3;
        set => AudioFormatInternal = value.GetStringValue();
    }

    /// <summary>Gets or sets the speech rate (e.g., "slow", "medium", "fast", or a numeric value).</summary>
    public string? SpeechRate { get; set; }

    /// <summary>Gets or sets the pitch (e.g., "low", "medium", "high", or a numeric value).</summary>
    public string? Pitch { get; set; }

    /// <summary>Gets or sets the volume (e.g., "soft", "medium", "loud", or a numeric value).</summary>
    public string? Volume { get; set; }

    public AwsPollyTtsRequest() { }

    public AwsPollyTtsRequest(string text, AwsPollyVoiceId? voiceId = null, LanguageCodeInfo? languageCode = null, AudioFormat? outputFormat = null)
    {
        Text = text;
        VoiceId = voiceId;
        LanguageCode = languageCode;
        OutputFormat = outputFormat ?? AudioFormat.Mp3;
    }

    public override string ToString()
    {
        var parts = new List<string> { $"Text: {Text.Substring(0, Math.Min(Text.Length, 50))}{(Text.Length > 50 ? "..." : "")}" };
        if (VoiceId.HasValue)
            parts.Add($"VoiceId: {VoiceId.Value.ToString()}");

        if (LanguageCode != null)
            parts.Add($"LanguageCode: {LanguageCode.Bcp47 ?? LanguageCode.Iso6391 ?? LanguageCode.Iso6393 ?? LanguageCode.Name}");

        parts.Add($"OutputFormat: {OutputFormat.GetStringValue()}");
        return string.Join(" | ", parts);
    }

    /// <summary>Parses audio format string to AudioFormat enum.</summary>
    private static AudioFormat? ParseAudioFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return null;

        var normalized = format.ToLowerInvariant().Trim();
        return Enum.TryParse<AudioFormat>(normalized, true, out var result) ? result : null;
    }
}