using System.ComponentModel;
using Lyo.Common.Attributes;

namespace Lyo.Common.Enums;

/// <summary>Represents common audio formats used for speech synthesis and recognition.</summary>
public enum AudioFormat
{
    /// <summary>Unknown or unspecified audio format</summary>
    [StringValue("unknown")]
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>WAV (Waveform Audio File Format)</summary>
    [StringValue("wav")]
    [Description("WAV (Waveform Audio File Format)")]
    Wav = 1,

    /// <summary>MP3 (MPEG Audio Layer III)</summary>
    [StringValue("mp3")]
    [Description("MP3 (MPEG Audio Layer III)")]
    Mp3 = 2,

    /// <summary>OGG (Ogg Vorbis)</summary>
    [StringValue("ogg")]
    [Description("OGG (Ogg Vorbis)")]
    Ogg = 3,

    /// <summary>FLAC (Free Lossless Audio Codec)</summary>
    [StringValue("flac")]
    [Description("FLAC (Free Lossless Audio Codec)")]
    Flac = 4,

    /// <summary>AAC (Advanced Audio Coding)</summary>
    [StringValue("aac")]
    [Description("AAC (Advanced Audio Coding)")]
    Aac = 5,

    /// <summary>M4A (MPEG-4 Audio)</summary>
    [StringValue("m4a")]
    [Description("M4A (MPEG-4 Audio)")]
    M4a = 6,

    /// <summary>OPUS (Opus audio codec)</summary>
    [StringValue("opus")]
    [Description("OPUS (Opus audio codec)")]
    Opus = 7,

    /// <summary>PCM (Pulse Code Modulation)</summary>
    [StringValue("pcm")]
    [Description("PCM (Pulse Code Modulation)")]
    Pcm = 8,

    /// <summary>WebM Audio</summary>
    [StringValue("webm")]
    [Description("WebM Audio")]
    Webm = 9
}