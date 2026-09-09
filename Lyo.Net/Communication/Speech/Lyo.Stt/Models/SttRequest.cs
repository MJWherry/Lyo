using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Enums;
using Lyo.Common.Metadata.Extensions;
using Lyo.Common.Metadata.JsonConverters;
using Lyo.Common.Metadata.Records;

namespace Lyo.Stt.Models;

/// <summary>An STT request: audio plus recognition options.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SttRequest
{
    /// <summary>Audio bytes to transcribe.</summary>
    public byte[]? AudioData { get; set; }

    /// <summary>Path of an audio file to transcribe.</summary>
    public string? AudioFilePath { get; set; }

    /// <summary>Language (for example "en-US" or "en-GB").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? LanguageCode { get; set; }

    /// <summary>Audio format (for example "wav", "mp3", or "flac").</summary>
    public AudioFormat? AudioFormat { get; set; }

    /// <summary>Sample rate, in Hz.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Channel count.</summary>
    public int? Channels { get; set; }

    /// <summary>Whether the transcript should include punctuation.</summary>
    public bool? EnablePunctuation { get; set; }

    /// <summary>Whether speaker diarization is enabled.</summary>
    public bool? EnableSpeakerDiarization { get; set; }

    public SttRequest() { }

    public SttRequest(byte[] audioData, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null)
    {
        AudioData = audioData;
        LanguageCode = languageCode;
        AudioFormat = audioFormat;
    }

    public SttRequest(string audioFilePath, LanguageCodeInfo? languageCode = null, AudioFormat? audioFormat = null)
    {
        AudioFilePath = audioFilePath;
        LanguageCode = languageCode;
        AudioFormat = audioFormat;
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (AudioData != null)
            parts.Add($"AudioData: {AudioData.Length} bytes");

        if (!string.IsNullOrWhiteSpace(AudioFilePath))
            parts.Add($"AudioFilePath: {AudioFilePath}");

        if (LanguageCode != null)
            parts.Add($"LanguageCode: {LanguageCode.Bcp47}");

        if (AudioFormat.HasValue)
            parts.Add($"AudioFormat: {AudioFormat.Value.GetStringValue()}");

        if (SampleRate.HasValue)
            parts.Add($"SampleRate: {SampleRate}Hz");

        if (Channels.HasValue)
            parts.Add($"Channels: {Channels}");

        return string.Join(" | ", parts);
    }
}