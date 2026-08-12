using System.Text.Json.Serialization;
using Lyo.Common.Enums;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;
using Lyo.Tts.Models;
using Lyo.Typecast.Client.Enums;
#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Request model for Typecast text-to-speech synthesis.</summary>
public class TypecastTtsRequest : TtsRequest
{
    /// <summary>Voice model to use for speech synthesis.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; } = TypecastModel.SsfmV30;

    /// <summary>Gets or sets the voice identifier.</summary>
    [JsonPropertyName("voice_id")]
    public string? VoiceId {
        get => VoiceIdInternal;
        set => VoiceIdInternal = value;
    }

    /// <summary>Gets or sets the language code (ISO 639-3 format, e.g., "eng", "kor", "jpn").</summary>
    [JsonPropertyName("language")]
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? Language {
        get {
            if (string.IsNullOrWhiteSpace(LanguageInternal))
                return null;

            var lang = LanguageCodeInfo.FromIso6393(LanguageInternal);
            return lang == LanguageCodeInfo.Unknown ? null : lang;
        }
        set => LanguageInternal = value?.Iso6393;
    }

    /// <summary>Emotion and style settings for the generated speech.</summary>
    [JsonPropertyName("prompt")]
    public Prompt? Prompt { get; set; }

    /// <summary>Audio output settings including volume, pitch, tempo, and format.</summary>
    [JsonPropertyName("output")]
    public OutputSettings? Output { get; set; }

    /// <summary>Random seed for controlling speech generation variations.</summary>
    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    /// <summary>Gets or sets the audio format (derived from Output settings).</summary>
    /// <remarks>This property is computed from the Output.AudioFormat value.</remarks>
    public AudioFormat AudioFormat => Enum.TryParse<AudioFormat>(Output?.AudioFormat, out var result) ? result : AudioFormat.Mp3;
}