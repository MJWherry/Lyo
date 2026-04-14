using System.Diagnostics;

namespace Lyo.Ffmpeg.Models;

/// <summary>Options for audio conversion when using stream/bytes overloads (codec, format, etc.).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class AudioConversionOptions
{
    /// <summary>Audio codec (e.g., "pcm_s16le", "libmp3lame").</summary>
    public string? Codec { get; set; }

    /// <summary>Sample rate in Hz.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Number of audio channels.</summary>
    public int? Channels { get; set; }

    /// <summary>Output format (e.g., "wav", "mp3").</summary>
    public string? Format { get; set; }

    /// <summary>Whether to overwrite output. Default: true</summary>
    public bool Overwrite { get; set; } = true;

    /// <summary>Whether to strip video (audio only). Default: true</summary>
    public bool NoVideo { get; set; } = true;
}