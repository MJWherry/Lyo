using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;
using Lyo.Tts.Models;

namespace Lyo.Tts.AwsPolly;

/// <summary>A Polly TTS request: text, voice, and output format.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class AwsPollyTtsRequest : TtsRequest
{
    /// <summary>Polly voice to use.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AwsPollyVoiceId? VoiceId {
        get => string.IsNullOrWhiteSpace(VoiceIdInternal) ? null : Enum.TryParse<AwsPollyVoiceId>(VoiceIdInternal, true, out var v) ? v : null;
        set => VoiceIdInternal = value?.ToString();
    }

    /// <summary>Language in BCP 47 form (for example "en-US" or "en-GB").</summary>
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

    /// <summary>Output format (for example "mp3", "wav", or "ogg").</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AudioFormat OutputFormat {
        get => ParseAudioFormat(AudioFormatInternal) ?? AudioFormat.Mp3;
        set => AudioFormatInternal = value.GetStringValue();
    }

    /// <summary>Speech rate ("slow", "medium", "fast", or a number).</summary>
    public string? SpeechRate { get; set; }

    /// <summary>Pitch ("low", "medium", "high", or a number).</summary>
    public string? Pitch { get; set; }

    /// <summary>Volume ("soft", "medium", "loud", or a number).</summary>
    public string? Volume { get; set; }

    /// <summary>Empty request; assign <see cref="TtsRequest.Text" /> and any optional fields before synthesizing.</summary>
    public AwsPollyTtsRequest() { }

    /// <summary>Builds a request with the usual fields filled in.</summary>
    /// <param name="text">Text to speak.</param>
    /// <param name="voiceId">Optional Polly voice.</param>
    /// <param name="languageCode">Optional BCP 47 language hint (mainly when no voice is chosen).</param>
    /// <param name="outputFormat">Audio container/codec; MP3 when omitted.</param>
    public AwsPollyTtsRequest(string text, AwsPollyVoiceId? voiceId = null, LanguageCodeInfo? languageCode = null, AudioFormat? outputFormat = null)
    {
        Text = text;
        VoiceId = voiceId;
        LanguageCode = languageCode;
        OutputFormat = outputFormat ?? AudioFormat.Mp3;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string> { $"Text: {Text[..Math.Min(Text.Length, 50)]}{(Text.Length > 50 ? "..." : "")}" };
        if (VoiceId.HasValue)
            parts.Add($"VoiceId: {VoiceId.Value.ToString()}");

        if (LanguageCode != null)
            parts.Add($"LanguageCode: {LanguageCode.Bcp47}");

        parts.Add($"OutputFormat: {OutputFormat.GetStringValue()}");
        return string.Join(" | ", parts);
    }

    /// <summary>Maps an audio-format string to AudioFormat.</summary>
    private static AudioFormat? ParseAudioFormat(string? format)
    {
        if (format.IsNullOrWhitespace())
            return null;

        var normalized = format.ToLowerInvariant().Trim();
        return Enum.TryParse<AudioFormat>(normalized, true, out var result) ? result : null;
    }
}