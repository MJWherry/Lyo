using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Tts.Models;
using Lyo.Typecast.Client.Enums;
#if NETSTANDARD2_0
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#endif

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Request model for Typecast text-to-speech synthesis.</summary>
public class TtsReq : TtsRequest
{
    /// <summary>Voice model to use for speech synthesis.</summary>
    //[JsonPropertyName("model")]
    public string? Model { get; set; } = TypecastModel.SsfmV30;

    /// <summary>Language code following ISO 639-3 standard (e.g., "eng", "kor", "jpn"). Case-insensitive. If not provided, will be auto-detected.</summary>
    //[JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>Emotion and style settings for the generated speech.</summary>
    //[JsonPropertyName("prompt")]
    public Prompt? Prompt { get; set; }

    /// <summary>Audio output settings including volume, pitch, tempo, and format.</summary>
    //[JsonPropertyName("output")]
    public OutputSettings? Output { get; set; }

    /// <summary>Random seed for controlling speech generation variations.</summary>
    //[JsonPropertyName("seed")]
    public int? Seed { get; set; }

    /// <summary>Parses ISO 639-3 language code to LanguageCodeInfo.</summary>
    private static LanguageCodeInfo? ParseLanguageCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        // Typecast uses ISO 639-3 codes (e.g., "eng", "kor", "jpn")
        // Map to LanguageCodeInfo using the registry
        var normalized = language.ToLowerInvariant().Trim();
        var langInfo = LanguageCodeInfo.FromIso6393(normalized);
        if (langInfo == LanguageCodeInfo.Unknown) {
            // Fallback to ISO 639-1
            langInfo = LanguageCodeInfo.FromIso6391(normalized);
        }

        return langInfo != LanguageCodeInfo.Unknown ? langInfo : null;
    }

    /// <summary>Parses audio format string to AudioFormat enum.</summary>
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