using System.Text.Json.Serialization;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;
using Lyo.Tts.Models;
using Lyo.Typecast.Client.Enums;
#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#endif

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Request payload for Typecast text-to-speech synthesis.</summary>
public class TypecastTtsRequest : TtsRequest
{
    /// <summary>Speech synthesis voice model to use.</summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; } = TypecastModel.SsfmV30;

    /// <summary>Holds the voice identifier.</summary>
    [JsonPropertyName("voice_id")]
    public string? VoiceId {
        get => VoiceIdInternal;
        set => VoiceIdInternal = value;
    }

    /// <summary>Language code (ISO 639-3 format, e.g., "eng", "kor", "jpn") for this record.</summary>
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

    /// <summary>The generated speech emotion and style settings.</summary>
    [JsonPropertyName("prompt")]
    public Prompt? Prompt { get; set; }

    /// <summary>Audio output: volume, pitch, tempo, and format.</summary>
    [JsonPropertyName("output")]
    public OutputSettings? Output { get; set; }

    /// <summary>Controlling speech generation variations random seed.</summary>
    [JsonPropertyName("seed")]
    public int? Seed { get; set; }

    /// <summary>Holds the audio format (derived from Output settings).</summary>
    /// <remarks>Derived from Output.AudioFormat.</remarks>
    public AudioFormat AudioFormat => Enum.TryParse<AudioFormat>(Output?.AudioFormat, out var result) ? result : AudioFormat.Mp3;
}