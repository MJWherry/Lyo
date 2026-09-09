using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.FFmpeg.Models;

/// <summary>What to do with process stdout and stderr that are not media or progress.</summary>
public enum FFmpegProcessOutputMode
{
    /// <summary>Keep output internally and do not print it to the console.</summary>
    Suppress = 0,

    /// <summary>Print output to the console as it arrives. Useful while debugging.</summary>
    Passthrough = 1
}

/// <summary>Settings that control how the FFmpeg services run. Frozen after <see cref="Validate" /> at DI; do not mutate from a convert.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class FFmpegOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "FFmpegOptions";

    /// <summary>Path to the ffmpeg binary. Null means <c>ffmpeg</c> on PATH is used.</summary>
    public string? FFmpegPath { get; set; }

    /// <summary>Path to the ffprobe binary. Null means <c>ffprobe</c> on PATH is used.</summary>
    public string? FfprobePath { get; set; }

    /// <summary>Path to the ffplay binary. Null means <c>ffplay</c> is used, or a path derived from FFmpegPath.</summary>
    public string? FfplayPath { get; set; }

    /// <summary>Audio codec used when <c>IAudioConverter</c> is called with no options. Starts as pcm_s16le.</summary>
    public string DefaultCodec { get; set; } = "pcm_s16le";

    /// <summary>Sample rate in hertz used when audio convert is called with no options. Starts at 44100.</summary>
    public int DefaultSampleRate { get; set; } = 44100;

    /// <summary>Channel count used when audio convert is called with no options. Starts at 2.</summary>
    public int DefaultChannels { get; set; } = 2;

    /// <summary>Output container used when audio convert is called with no options. Starts as wav.</summary>
    public string DefaultFormat { get; set; } = "wav";

    /// <summary>Overwrite behavior when a request does not set one. Starts as true (<c>-y</c>).</summary>
    public bool DefaultOverwrite { get; set; } = true;

    /// <summary>Extra ffmpeg arguments prepended to every command, such as <c>-hide_banner</c> or <c>-loglevel warning</c>.</summary>
    public IReadOnlyList<string>? GlobalArguments { get; set; }

    /// <summary>If true, FFmpeg operations record metrics. Starts as false.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>If true, ffplay version, config, and progress lines are hidden. Starts as true.</summary>
    public bool SuppressFfplayOutput { get; set; } = true;

    /// <summary>How leftover stdout/stderr is handled. Starts as Suppress.</summary>
    public FFmpegProcessOutputMode ProcessOutputMode { get; set; } = FFmpegProcessOutputMode.Suppress;

    /// <summary>
    /// Wall-clock timeout for batch convert, probe, and play. Null means no timeout. Do not set this for StartConvertAsync sessions;
    /// a live stream can run for hours and is cancelled with the token or by disposing the session.
    /// </summary>
    public TimeSpan? ProcessTimeout { get; set; }

    /// <summary>Checks sample rate, channels, enum, and timeout.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfLessThan(DefaultSampleRate, 1);
        ArgumentHelpers.ThrowIfLessThan(DefaultChannels, 1);
        ArgumentHelpers.ThrowIfNotDefined(ProcessOutputMode);
        ArgumentHelpers.ThrowIf(ProcessTimeout is { } timeout && timeout <= TimeSpan.Zero, "ProcessTimeout must be null or greater than zero.", nameof(ProcessTimeout));
    }

    /// <inheritdoc />
    public override string ToString() => $"FFmpegOptions: FFmpegPath={FFmpegPath ?? "ffmpeg"}, DefaultCodec={DefaultCodec}, DefaultFormat={DefaultFormat}";
}
