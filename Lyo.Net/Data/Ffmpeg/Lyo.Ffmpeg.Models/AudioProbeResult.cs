using System.Diagnostics;

namespace Lyo.Ffmpeg.Models;

/// <summary>Result of probing an audio/video file for metadata.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class AudioProbeResult
{
    /// <summary>Gets or sets the file path that was probed.</summary>
    public string FilePath { get; set; } = null!;

    /// <summary>Gets or sets the duration in seconds.</summary>
    public double? DurationSeconds { get; set; }

    /// <summary>Gets or sets the format name (e.g., "wav", "mp3").</summary>
    public string? Format { get; set; }

    /// <summary>Gets or sets the sample rate in Hz.</summary>
    public int? SampleRate { get; set; }

    /// <summary>Gets or sets the number of audio channels.</summary>
    public int? Channels { get; set; }

    /// <summary>Gets or sets the codec name.</summary>
    public string? Codec { get; set; }

    /// <summary>Gets or sets the bit rate in bits per second.</summary>
    public long? BitRate { get; set; }

    /// <summary>Gets or sets the file size in bytes.</summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>Gets or sets whether the file contains video.</summary>
    public bool HasVideo { get; set; }

    /// <summary>Gets or sets whether the file contains audio.</summary>
    public bool HasAudio { get; set; }

    /// <summary>Gets or sets additional raw metadata from ffprobe.</summary>
    public IReadOnlyDictionary<string, string>? RawMetadata { get; set; }

    public override string ToString() => $"Probe: {FilePath} (Duration={DurationSeconds}s, Format={Format}, {SampleRate}Hz, {Channels}ch)";
}