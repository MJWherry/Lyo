using System.Text.Json.Serialization;

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Audio output settings.</summary>
public class OutputSettings
{
    /// <summary>Volume level (0-200). Default: 100.</summary>
    [JsonPropertyName("volume")]
    public int? Volume { get; set; }

    /// <summary>Audio pitch in semitones (-12 to +12). Default: 0.</summary>
    [JsonPropertyName("audio_pitch")]
    public int? AudioPitch { get; set; }

    /// <summary>Audio tempo multiplier (0.5x to 2.0x). Default: 1.</summary>
    [JsonPropertyName("audio_tempo")]
    public double? AudioTempo { get; set; }

    /// <summary>Audio format. Options: "wav" or "mp3". Default: "wav".</summary>
    [JsonPropertyName("audio_format")]
    public string? AudioFormat { get; set; }
}