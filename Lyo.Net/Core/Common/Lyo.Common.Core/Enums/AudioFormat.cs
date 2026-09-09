using System.ComponentModel;
using Lyo.Common.Core.Attributes;

namespace Lyo.Common.Core.Enums;

/// <summary>Audio encodings used for speech synthesis and recognition.</summary>
public enum AudioFormat
{
    /// <summary>Audio format is not known or not set</summary>
    [StringValue("unknown")]
    [Description("Unknown")]
    Unknown = 0,

    /// <summary>Waveform Audio File Format (WAV)</summary>
    [StringValue("wav")]
    [Description("WAV (Waveform Audio File Format)")]
    Wav = 1,

    /// <summary>MPEG Audio Layer III (MP3)</summary>
    [StringValue("mp3")]
    [Description("MP3 (MPEG Audio Layer III)")]
    Mp3 = 2,

    /// <summary>Ogg Vorbis (OGG)</summary>
    [StringValue("ogg")]
    [Description("OGG (Ogg Vorbis)")]
    Ogg = 3,

    /// <summary>Free Lossless Audio Codec (FLAC)</summary>
    [StringValue("flac")]
    [Description("FLAC (Free Lossless Audio Codec)")]
    Flac = 4,

    /// <summary>Advanced Audio Coding (AAC)</summary>
    [StringValue("aac")]
    [Description("AAC (Advanced Audio Coding)")]
    Aac = 5,

    /// <summary>MPEG-4 Audio (M4A)</summary>
    [StringValue("m4a")]
    [Description("M4A (MPEG-4 Audio)")]
    M4a = 6,

    /// <summary>Opus audio codec (OPUS)</summary>
    [StringValue("opus")]
    [Description("OPUS (Opus audio codec)")]
    Opus = 7,

    /// <summary>Pulse Code Modulation (PCM)</summary>
    [StringValue("pcm")]
    [Description("PCM (Pulse Code Modulation)")]
    Pcm = 8,

    /// <summary>WebM audio container</summary>
    [StringValue("webm")]
    [Description("WebM Audio")]
    Webm = 9
}