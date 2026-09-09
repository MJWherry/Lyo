using System.Text.Json.Serialization;

namespace Lyo.Typecast.Client.Models.TextToSpeech.Request;

/// <summary>Settings for generated audio.</summary>
public class OutputSettings
{
    /// <summary>Volume in 0–200. Default: 100.</summary>
    [JsonPropertyName("volume")]
    public int? Volume { get; set; }

    /// <summary>Pitch in semitones from -12 to +12. Default: 0.</summary>
    [JsonPropertyName("audio_pitch")]
    public int? AudioPitch { get; set; }

    /// <summary>Tempo multiplier from 0.5x to 2.0x. Default: 1.</summary>
    [JsonPropertyName("audio_tempo")]
    public double? AudioTempo { get; set; }

    /// <summary>Audio format: "wav" or "mp3". Default: "wav".</summary>
    [JsonPropertyName("audio_format")]
    public string? AudioFormat { get; set; }
}