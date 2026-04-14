using System.Diagnostics;

namespace Lyo.Ffmpeg.Models;

/// <summary>How process stdout/stderr should be handled.</summary>
public enum FfmpegProcessOutputMode
{
    /// <summary>Suppress output (capture internally only, do not echo to console).</summary>
    Suppress = 0,

    /// <summary>Echo output to console as it arrives (for debugging).</summary>
    Passthrough = 1
}

/// <summary>Options for configuring FFmpeg service behavior.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class FfmpegOptions
{
    public const string SectionName = "FfmpegOptions";

    /// <summary>Path to the ffmpeg executable. If null, "ffmpeg" is used (must be in PATH).</summary>
    public string? FfmpegPath { get; set; }

    /// <summary>Path to the ffprobe executable. If null, "ffprobe" is used (must be in PATH).</summary>
    public string? FfprobePath { get; set; }

    /// <summary>Path to the ffplay executable. If null, "ffplay" is used or derived from FfmpegPath.</summary>
    public string? FfplayPath { get; set; }

    /// <summary>Default audio codec when not specified. Default: pcm_s16le</summary>
    public string DefaultCodec { get; set; } = "pcm_s16le";

    /// <summary>Default sample rate in Hz when not specified. Default: 44100</summary>
    public int DefaultSampleRate { get; set; } = 44100;

    /// <summary>Default number of channels when not specified. Default: 2</summary>
    public int DefaultChannels { get; set; } = 2;

    /// <summary>Default output format when not specified. Default: wav</summary>
    public string DefaultFormat { get; set; } = "wav";

    /// <summary>Default overwrite behavior. Default: true</summary>
    public bool DefaultOverwrite { get; set; } = true;

    /// <summary>Default no-video (audio only) behavior. Default: true</summary>
    public bool DefaultNoVideo { get; set; } = true;

    /// <summary>Additional global ffmpeg arguments to prepend (e.g., "-hide_banner", "-loglevel warning").</summary>
    public IReadOnlyList<string>? GlobalArguments { get; set; }

    /// <summary>Enable metrics collection for FFmpeg operations. Default: false</summary>
    public bool EnableMetrics { get; set; } = false;

    /// <summary>Suppress ffplay console output (version, config, progress). Default: true</summary>
    public bool SuppressFfplayOutput { get; set; } = true;

    /// <summary>How ffmpeg/ffprobe stdout and stderr are handled. Default: Suppress</summary>
    public FfmpegProcessOutputMode ProcessOutputMode { get; set; } = FfmpegProcessOutputMode.Suppress;

    public override string ToString() => $"FfmpegOptions: FfmpegPath={FfmpegPath ?? "ffmpeg"}, DefaultCodec={DefaultCodec}, DefaultFormat={DefaultFormat}";
}