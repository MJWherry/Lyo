using Lyo.Common.Core.Records;
using Lyo.FileTags;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>Internal convert knobs shared by audio and video facades. Builder maps catalog Format/Id onto ffmpeg argv.</summary>
internal sealed record ConvertKnobs
{
    public MediaContainer? Format { get; init; }

    public bool Overwrite { get; init; } = true;

    public AudioEncoder? AudioEncoder { get; init; }

    public AudioSampleRate? SampleRate { get; init; }

    public ChannelLayout? Channels { get; init; }

    public MediaBitRate? AudioBitRate { get; init; }

    public AudioVbrQuality? AudioQuality { get; init; }

    public string? AudioFilter { get; init; }

    public VideoEncoder? VideoEncoder { get; init; }

    public VideoFrameRate? FrameRate { get; init; }

    public PixelFormat? PixelFormat { get; init; }

    public ConstantRateFactor? Crf { get; init; }

    public MediaBitRate? VideoBitRate { get; init; }

    public EncoderPreset? Preset { get; init; }

    public string? VideoFilter { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public ScaleMode Scale { get; init; }

    public bool DropVideo { get; init; }

    public bool DropAudio { get; init; }

    public bool CopyCodecs { get; init; }

    public TimeSpan? StartTime { get; init; }

    public TimeSpan? Duration { get; init; }

    public TimeSpan? KnownDuration { get; init; }

    public IProgress<MediaProgress>? Progress { get; init; }

    public MediaIoMode IoMode { get; init; }

    public bool FragmentedOutput { get; init; }

    public MediaContainer? InputFormat { get; init; }

    public AudioSampleRate? InputSampleRate { get; init; }

    public ChannelLayout? InputChannels { get; init; }

    public HardwareAccel? Hardware { get; init; }

    public HardwareDevice? HardwareDevice { get; init; }

    public IReadOnlyList<MediaStreamMap>? Maps { get; init; }

    public IReadOnlyDictionary<FileTagKey, string>? Metadata { get; init; }

    public bool ClearMetadata { get; init; }

    public string? SubtitlePath { get; init; }

    public SubtitleMode Subtitles { get; init; }

    public SubtitleCodec? SubtitleCodec { get; init; }

    public LoudnessTarget? Loudness { get; init; }

    public bool TwoPass { get; init; }

    public StreamPackaging? Packaging { get; init; }

    public bool ConcatSafe { get; init; }

    public static ConvertKnobs FromAudio(AudioConversionOptions options)
        => new() {
            Format = options.Format,
            Overwrite = options.Overwrite,
            AudioEncoder = options.Encoder,
            SampleRate = options.SampleRate,
            Channels = options.Channels,
            AudioBitRate = options.AudioBitRate,
            AudioQuality = options.AudioQuality,
            AudioFilter = options.AudioFilter,
            DropVideo = true,
            CopyCodecs = options.CopyCodecs,
            StartTime = options.StartTime,
            Duration = options.Duration,
            KnownDuration = options.KnownDuration,
            Progress = options.Progress,
            IoMode = options.IoMode,
            InputFormat = options.InputFormat,
            InputSampleRate = options.InputSampleRate,
            InputChannels = options.InputChannels,
            Metadata = options.Metadata,
            ClearMetadata = options.ClearMetadata,
            Loudness = options.Loudness
        };

    public static ConvertKnobs FromVideo(VideoConversionOptions options)
        => new() {
            Format = options.Format,
            Overwrite = options.Overwrite,
            AudioEncoder = options.AudioEncoder,
            VideoEncoder = options.Encoder,
            FrameRate = options.FrameRate,
            PixelFormat = options.PixelFormat,
            Crf = options.Crf,
            VideoBitRate = options.VideoBitRate,
            Preset = options.Preset,
            VideoFilter = options.VideoFilter,
            Width = options.Width,
            Height = options.Height,
            Scale = options.Scale,
            DropAudio = options.DropAudio,
            CopyCodecs = options.CopyCodecs,
            StartTime = options.StartTime,
            Duration = options.Duration,
            KnownDuration = options.KnownDuration,
            Progress = options.Progress,
            IoMode = options.IoMode,
            FragmentedOutput = options.FragmentedOutput,
            InputFormat = options.InputFormat,
            AudioBitRate = options.AudioBitRate,
            AudioQuality = options.AudioQuality,
            SampleRate = options.SampleRate,
            Channels = options.Channels,
            AudioFilter = options.AudioFilter,
            Hardware = options.Hardware,
            HardwareDevice = options.HardwareDevice,
            Maps = options.Maps,
            Metadata = options.Metadata,
            ClearMetadata = options.ClearMetadata,
            SubtitlePath = options.SubtitlePath,
            Subtitles = options.Subtitles,
            SubtitleCodec = options.SubtitleCodec,
            Loudness = options.Loudness,
            TwoPass = options.TwoPass,
            Packaging = options.Packaging
        };
}