using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Common.JsonConverters;
using Lyo.Common.Records;

namespace Lyo.Stt.Models;

/// <summary>Represents a Speech-to-Text request with audio data and recognition options.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SttRequest
{
    /// <summary>Gets or sets the audio data to transcribe.</summary>
    public byte[]? AudioData { get; set; }

    /// <summary>Gets or sets the audio file path to transcribe.</summary>
    public string? AudioFilePath { get; set; }

    /// <summary>Gets or sets the language code (e.g., "en-US", "en-GB").</summary>
    [JsonConverter(typeof(NullableLanguageCodeInfoJsonConverter))]
    public LanguageCodeInfo? LanguageCode { get; set; }

    /// <summary>Gets or sets the audio format (e.g., "wav", "mp3", "flac").</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AudioFormat? AudioFormat { get; set; }

    /// <summary>Gets or sets the sample rate in Hz.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Gets or sets the number of audio channels.</summary>
    public int? Channels { get; set; }

    /// <summary>Gets or sets whether to enable punctuation in the transcription.</summary>
    public bool? EnablePunctuation { get; set; }

    /// <summary>Gets or sets whether to enable speaker diarization.</summary>
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
            parts.Add($"LanguageCode: {LanguageCode.Bcp47 ?? LanguageCode.Iso6391 ?? LanguageCode.Iso6393 ?? LanguageCode.Name}");

        if (AudioFormat.HasValue)
            parts.Add($"AudioFormat: {AudioFormat.Value.GetStringValue()}");

        if (SampleRate.HasValue)
            parts.Add($"SampleRate: {SampleRate}Hz");

        if (Channels.HasValue)
            parts.Add($"Channels: {Channels}");

        return string.Join(" | ", parts);
    }
}