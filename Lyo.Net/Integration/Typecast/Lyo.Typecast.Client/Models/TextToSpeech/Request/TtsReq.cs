using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Records;
using Lyo.Tts.Models;
using Lyo.Typecast.Client.Enums;
#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#endif

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Request payload for Typecast text-to-speech synthesis.</summary>
public class TtsReq : TtsRequest
{
    /// <summary>Speech synthesis voice model to use.</summary>
    //[JsonPropertyName("model")]
    public string? Model { get; set; } = TypecastModel.SsfmV30;

    /// <summary>ISO 639-3 language code (e.g., "eng", "kor", "jpn"). Case-insensitive. If not provided, will be auto-detected.</summary>
    //[JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>The generated speech emotion and style settings.</summary>
    //[JsonPropertyName("prompt")]
    public Prompt? Prompt { get; set; }

    /// <summary>Audio output: volume, pitch, tempo, and format.</summary>
    //[JsonPropertyName("output")]
    public OutputSettings? Output { get; set; }

    /// <summary>Controlling speech generation variations random seed.</summary>
    //[JsonPropertyName("seed")]
    public int? Seed { get; set; }

    /// <summary>Converts an ISO 639-3 code into LanguageCodeInfo.</summary>
    private static LanguageCodeInfo? ParseLanguageCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        // Typecast language codes are ISO 639-3 (for example "eng", "kor", "jpn")
        // Resolve LanguageCodeInfo through the registry
        var normalized = language.ToLowerInvariant().Trim();
        var langInfo = LanguageCodeInfo.FromIso6393(normalized);
        if (langInfo == LanguageCodeInfo.Unknown) {
            // Use ISO 639-1 when the 639-3 lookup misses
            langInfo = LanguageCodeInfo.FromIso6391(normalized);
        }

        return langInfo != LanguageCodeInfo.Unknown ? langInfo : null;
    }

    /// <summary>Converts an audio-format string into AudioFormat.</summary>
    private static AudioFormat ParseAudioFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return AudioFormat.Wav; // Typecast defaults to WAV

        var normalized = format.ToLowerInvariant().Trim();
        return normalized switch {
            "wav" => AudioFormat.Wav,
            "mp3" => AudioFormat.Mp3,
            "ogg" => AudioFormat.Ogg,
            "flac" => AudioFormat.Flac,
            "aac" => AudioFormat.Aac,
            "m4a" => AudioFormat.M4a,
            "opus" => AudioFormat.Opus,
            "pcm" => AudioFormat.Pcm,
            "webm" => AudioFormat.Webm,
            var _ => AudioFormat.Unknown
        };
    }
}