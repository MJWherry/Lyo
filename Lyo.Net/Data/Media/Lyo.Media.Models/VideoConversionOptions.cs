using System.Diagnostics;
using Lyo.Common.Core.Records;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>
/// Convert knobs for <see cref="IVideoConverter" />.
/// A bare instance does not force a codec or size.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record VideoConversionOptions
{
    /// <summary>Output container.</summary>
    public MediaContainer? Format { get; init; }

    /// <summary>If true, an existing output is overwritten. If false, the convert fails when the output exists. Starts as true.</summary>
    public bool Overwrite { get; init; } = true;

    /// <summary>Video codec.</summary>
    public VideoEncoder? Encoder { get; init; }

    /// <summary>Audio codec when the output keeps an audio stream.</summary>
    public AudioEncoder? AudioEncoder { get; init; }

    /// <summary>Output frame rate.</summary>
    public double? FrameRate { get; init; }

    /// <summary>Pixel format.</summary>
    public PixelFormat? PixelFormat { get; init; }

    /// <summary>Constant rate factor. Mutually exclusive with <see cref="VideoBitRate" />.</summary>
    public int? Crf { get; init; }

    /// <summary>Video bitrate, for example <c>2M</c>. Mutually exclusive with <see cref="Crf" />.</summary>
    public string? VideoBitRate { get; init; }

    /// <summary>Encoder speed/quality preset.</summary>
    public EncoderPreset? Preset { get; init; }

    /// <summary>Video filtergraph.</summary>
    public string? VideoFilter { get; init; }

    /// <summary>Output width. Must be paired with <see cref="Height" />.</summary>
    public int? Width { get; init; }

    /// <summary>Output height. Must be paired with <see cref="Width" />.</summary>
    public int? Height { get; init; }

    /// <summary>If true, audio is dropped. Starts as false.</summary>
    public bool DropAudio { get; init; }

    /// <summary>If true, remux without re-encoding unless <see cref="Encoder" /> or <see cref="AudioEncoder" /> overrides a side.</summary>
    public bool CopyCodecs { get; init; }

    /// <summary>Input seek. Null means start at the beginning.</summary>
    public TimeSpan? StartTime { get; init; }

    /// <summary>Output duration. Null means encode to the end.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Known duration used only to compute <see cref="MediaProgress.Percentage" />. Does not probe the source.</summary>
    public TimeSpan? KnownDuration { get; init; }

    /// <summary>Progress reporter. Callbacks run on the process I/O thread. Null means the backend does not report progress.</summary>
    public IProgress<MediaProgress>? Progress { get; init; }

    /// <summary>How stream overloads feed the converter. Starts as <see cref="MediaIoMode.TempFile" />.</summary>
    public MediaIoMode IoMode { get; init; } = MediaIoMode.TempFile;

    /// <summary>If true, fragment the mp4 so it can stream on a pipe. Requires <see cref="MediaContainer.Mp4" />. Starts as false.</summary>
    public bool FragmentedOutput { get; init; }

    /// <summary>Input container, for raw or generated input.</summary>
    public MediaContainer? InputFormat { get; init; }

    /// <summary>Change container and optional codecs with no quality knobs.</summary>
    public static VideoConversionOptions ForTranscode(MediaContainer format, AudioEncoder? audioEncoder = null, VideoEncoder? videoEncoder = null)
    {
        ArgumentHelpers.ThrowIfNull(format);
        return new() { Format = format, AudioEncoder = audioEncoder, Encoder = videoEncoder };
    }

    /// <summary>Re-encode video smaller. CRF 23 and preset medium match usual x264 defaults.</summary>
    public static VideoConversionOptions ForCompressVideo(int crf = 23, EncoderPreset? preset = null, VideoEncoder? encoder = null, MediaContainer? format = null)
        => new() { Crf = crf, Preset = preset ?? EncoderPreset.Medium, Encoder = encoder ?? VideoEncoder.H264, Format = format ?? MediaContainer.Mp4 };

    /// <summary>
    /// Remux for a live stream. Copy codecs, pipe I/O. Defaults to fragmented mp4.
    /// Pass <see cref="MediaContainer.WebM" /> for copy plus pipe without fragmented mp4.
    /// </summary>
    public static VideoConversionOptions ForStreamingVideo(MediaContainer? format = null)
    {
        var container = format ?? MediaContainer.Mp4;
        return new() {
            CopyCodecs = true,
            IoMode = MediaIoMode.Pipe,
            Format = container,
            FragmentedOutput = container.IsMp4
        };
    }

    /// <summary>Checks mutually exclusive knobs. File-to-file plus <see cref="MediaIoMode.Pipe" /> is rejected by the converter, not here.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotDefined(IoMode);
        ArgumentHelpers.ThrowIf(DropAudio && AudioEncoder != null, "DropAudio cannot be combined with AudioEncoder.", nameof(DropAudio));
        ArgumentHelpers.ThrowIf(Crf != null && !string.IsNullOrWhiteSpace(VideoBitRate), "Set Crf or VideoBitRate, not both.", nameof(Crf));
        ArgumentHelpers.ThrowIf(Width.HasValue != Height.HasValue, "Width and Height must be set together.", nameof(Width));
        if (Width is { } width)
            ArgumentHelpers.ThrowIfLessThan(width, 1);
        if (Height is { } height)
            ArgumentHelpers.ThrowIfLessThan(height, 1);
        ArgumentHelpers.ThrowIfNotNullAndLessThanOrEqual(FrameRate, 0, message: "FrameRate must be positive.");
        ArgumentHelpers.ThrowIf(FragmentedOutput && Format != null && !Format.IsMp4, "FragmentedOutput requires Format Mp4.", nameof(FragmentedOutput));
    }

    /// <inheritdoc />
    public override string ToString()
        => $"VideoConversionOptions(Format={Format}, Encoder={Encoder}, IoMode={IoMode}, CopyCodecs={CopyCodecs}, FragmentedOutput={FragmentedOutput})";
}
