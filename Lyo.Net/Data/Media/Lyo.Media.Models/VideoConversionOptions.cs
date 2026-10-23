using System.Diagnostics;
using Lyo.Common.Core.Records;
using Lyo.Exceptions;
using Lyo.FileTags;

namespace Lyo.Media.Models;

/// <summary>Convert knobs for <see cref="IVideoConverter" />. A bare instance does not force a codec or size. A null <see cref="Hardware" /> inherits the host device.</summary>
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
    public VideoFrameRate? FrameRate { get; init; }

    /// <summary>Pixel format.</summary>
    public PixelFormat? PixelFormat { get; init; }

    /// <summary>Constant rate factor. Mutually exclusive with <see cref="VideoBitRate" />.</summary>
    public ConstantRateFactor? Crf { get; init; }

    /// <summary>Video bitrate. Mutually exclusive with <see cref="Crf" />.</summary>
    public MediaBitRate? VideoBitRate { get; init; }

    /// <summary>Encoder speed/quality preset.</summary>
    public EncoderPreset? Preset { get; init; }

    /// <summary>Video filtergraph.</summary>
    public string? VideoFilter { get; init; }

    /// <summary>Output width. <see cref="ScaleMode.Stretch" /> and <see cref="ScaleMode.Fill" /> also require <see cref="Height" />.</summary>
    public int? Width { get; init; }

    /// <summary>Output height. <see cref="ScaleMode.Stretch" /> and <see cref="ScaleMode.Fill" /> also require <see cref="Width" />.</summary>
    public int? Height { get; init; }

    /// <summary>How width and height are applied. Starts as <see cref="ScaleMode.Stretch" />.</summary>
    public ScaleMode Scale { get; init; } = ScaleMode.Stretch;

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

    /// <summary>Audio bitrate when the output keeps an audio stream. Mutually exclusive with <see cref="AudioQuality" />.</summary>
    public MediaBitRate? AudioBitRate { get; init; }

    /// <summary>Audio VBR quality. Mutually exclusive with <see cref="AudioBitRate" />.</summary>
    public AudioVbrQuality? AudioQuality { get; init; }

    /// <summary>Output sample rate when the output keeps an audio stream.</summary>
    public AudioSampleRate? SampleRate { get; init; }

    /// <summary>Output channel layout when the output keeps an audio stream.</summary>
    public ChannelLayout? Channels { get; init; }

    /// <summary>Audio filtergraph. Mutually exclusive with <see cref="Loudness" />.</summary>
    public string? AudioFilter { get; init; }

    /// <summary>Hardware device for this call. Null inherits the host. <see cref="HardwareAccel.None" /> forces the CPU.</summary>
    public HardwareAccel? Hardware { get; init; }

    /// <summary>VAAPI device path. Null uses the host path or <see cref="HardwareDevice.RenderD128" />.</summary>
    public HardwareDevice? HardwareDevice { get; init; }

    /// <summary>Stream selectors. Null means ffmpeg's default mapping.</summary>
    public IReadOnlyList<MediaStreamMap>? Maps { get; init; }

    /// <summary>Tags written with <c>-metadata</c>.</summary>
    public IReadOnlyDictionary<FileTagKey, string>? Metadata { get; init; }

    /// <summary>If true, existing container tags are dropped before <see cref="Metadata" /> is written.</summary>
    public bool ClearMetadata { get; init; }

    /// <summary>Subtitle file. Requires <see cref="Subtitles" /> other than <see cref="SubtitleMode.None" />.</summary>
    public string? SubtitlePath { get; init; }

    /// <summary>How <see cref="SubtitlePath" /> is applied.</summary>
    public SubtitleMode Subtitles { get; init; }

    /// <summary>Subtitle codec for <see cref="SubtitleMode.Copy" />. Null picks one from the output container.</summary>
    public SubtitleCodec? SubtitleCodec { get; init; }

    /// <summary>Two-pass loudness target for the audio stream. Mutually exclusive with <see cref="AudioFilter" />.</summary>
    public LoudnessTarget? Loudness { get; init; }

    /// <summary>Bitrate two-pass. Requires <see cref="VideoBitRate" /> and H.264 or HEVC. Rejects a GPU device.</summary>
    public bool TwoPass { get; init; }

    /// <summary>HLS or DASH packaging. File output only.</summary>
    public StreamPackaging? Packaging { get; init; }

    /// <summary>Change container and optional codecs with no quality knobs.</summary>
    public static VideoConversionOptions ForTranscode(MediaContainer format, AudioEncoder? audioEncoder = null, VideoEncoder? videoEncoder = null)
    {
        ArgumentHelpers.ThrowIfNull(format);
        return new() { Format = format, AudioEncoder = audioEncoder, Encoder = videoEncoder };
    }

    /// <summary>Re-encode video smaller. CRF 23 and preset medium match usual x264 defaults. A hardware device leaves the preset unset.</summary>
    public static VideoConversionOptions ForCompressVideo(
        ConstantRateFactor? crf = null,
        EncoderPreset? preset = null,
        VideoEncoder? encoder = null,
        MediaContainer? format = null,
        HardwareAccel? hardware = null)
    {
        var useHardware = hardware != null && hardware != HardwareAccel.None;
        return new() {
            Crf = crf ?? ConstantRateFactor.Standard,
            Preset = preset ?? (useHardware ? null : EncoderPreset.Medium),
            Encoder = encoder ?? VideoEncoder.H264,
            Format = format ?? MediaContainer.Mp4,
            Hardware = hardware
        };
    }

    /// <summary>Remux for a live stream. Copy codecs, pipe I/O. Defaults to fragmented mp4. Pass <see cref="MediaContainer.WebM" /> for copy plus pipe without fragmented mp4.</summary>
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

    /// <summary>One frame scaled into an image file. <see cref="ScaleMode.Fit" /> keeps the aspect ratio.</summary>
    public static VideoConversionOptions ForThumbnail(int width, int height, ScaleMode scale = ScaleMode.Fit)
    {
        ArgumentHelpers.ThrowIfLessThan(width, 1);
        ArgumentHelpers.ThrowIfLessThan(height, 1);
        ArgumentHelpers.ThrowIfNotDefined(scale);
        return new() { Width = width, Height = height, Scale = scale };
    }

    /// <summary>GIF with a palette. Default is 12 fps and a 480-wide fit.</summary>
    public static VideoConversionOptions ForGif(int? width = 480, VideoFrameRate? frameRate = null)
    {
        if (width is { } pixels)
            ArgumentHelpers.ThrowIfLessThan(pixels, 1);

        return new() {
            Format = MediaContainer.Gif,
            FrameRate = frameRate ?? VideoFrameRate.Fps12,
            Width = width,
            Scale = width == null ? ScaleMode.Stretch : ScaleMode.Fit
        };
    }

    /// <summary>HLS file output. Copies codecs unless <paramref name="copyCodecs" /> is false.</summary>
    public static VideoConversionOptions ForHls(StreamPackaging? packaging = null, bool copyCodecs = true)
        => new() { Format = MediaContainer.Hls, CopyCodecs = copyCodecs, Packaging = packaging ?? StreamPackaging.Hls() };

    /// <summary>DASH file output. Copies codecs unless <paramref name="copyCodecs" /> is false.</summary>
    public static VideoConversionOptions ForDash(StreamPackaging? packaging = null, bool copyCodecs = true)
        => new() { Format = MediaContainer.Dash, CopyCodecs = copyCodecs, Packaging = packaging ?? StreamPackaging.Dash() };

    /// <summary>Checks mutually exclusive knobs. File-to-file plus <see cref="MediaIoMode.Pipe" /> is rejected by the converter, not here.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNotDefined(IoMode);
        ArgumentHelpers.ThrowIfNotDefined(Scale);
        ArgumentHelpers.ThrowIfNotDefined(Subtitles);
        ArgumentHelpers.ThrowIf(DropAudio && AudioEncoder != null, "DropAudio cannot be combined with AudioEncoder.", nameof(DropAudio));
        ArgumentHelpers.ThrowIf(Crf != null && VideoBitRate != null, "Set Crf or VideoBitRate, not both.", nameof(Crf));
        ArgumentHelpers.ThrowIf(AudioBitRate != null && AudioQuality != null, "Set AudioBitRate or AudioQuality, not both.", nameof(AudioBitRate));
        ArgumentHelpers.ThrowIf(Loudness != null && !string.IsNullOrWhiteSpace(AudioFilter), "Set Loudness or AudioFilter, not both.", nameof(Loudness));
        ArgumentHelpers.ThrowIf(Subtitles != SubtitleMode.None && string.IsNullOrWhiteSpace(SubtitlePath), "Subtitle mode requires SubtitlePath.", nameof(SubtitlePath));
        ArgumentHelpers.ThrowIf(TwoPass && VideoBitRate == null, "TwoPass requires VideoBitRate.", nameof(TwoPass));
        ArgumentHelpers.ThrowIf(TwoPass && Crf != null, "TwoPass cannot be combined with Crf.", nameof(TwoPass));
        ArgumentHelpers.ThrowIf(TwoPass && Hardware != null && Hardware != HardwareAccel.None, "TwoPass cannot use hardware.", nameof(TwoPass));
        ArgumentHelpers.ThrowIf(TwoPass && Encoder != null && Encoder != VideoEncoder.H264 && Encoder != VideoEncoder.Hevc, "TwoPass requires H264 or Hevc.", nameof(TwoPass));
        ValidateSize();
        ArgumentHelpers.ThrowIf(FragmentedOutput && Format != null && !Format.IsMp4, "FragmentedOutput requires Format Mp4.", nameof(FragmentedOutput));
        ArgumentHelpers.ThrowIf(Encoder == VideoEncoder.Av1 && Preset is { IsX264: true }, "AV1 does not accept an x264 preset. Use EncoderPreset.Svt8.", nameof(Preset));
    }

    /// <inheritdoc />
    public override string ToString()
        => $"VideoConversionOptions(Format={Format}, Encoder={Encoder}, IoMode={IoMode}, CopyCodecs={CopyCodecs}, FragmentedOutput={FragmentedOutput})";

    private void ValidateSize()
    {
        if (Width is { } width)
            ArgumentHelpers.ThrowIfLessThan(width, 1);

        if (Height is { } height)
            ArgumentHelpers.ThrowIfLessThan(height, 1);

        var oneSide = Width.HasValue != Height.HasValue;
        if (Scale == ScaleMode.Fit)
            return;

        ArgumentHelpers.ThrowIf(oneSide, "Width and Height must be set together.", nameof(Width));
    }
}