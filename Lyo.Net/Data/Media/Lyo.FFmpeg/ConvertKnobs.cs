using Lyo.Common.Core.Records;
using Lyo.Media.Models;

namespace Lyo.FFmpeg;

/// <summary>Internal convert knobs shared by audio and video facades. Builder maps catalog Format/Id onto ffmpeg argv.</summary>
internal sealed record ConvertKnobs
{
    public MediaContainer? Format { get; init; }

    public bool Overwrite { get; init; } = true;

    public AudioEncoder? AudioEncoder { get; init; }

    public int? SampleRate { get; init; }

    public int? Channels { get; init; }

    public string? AudioBitRate { get; init; }

    public string? AudioQuality { get; init; }

    public string? AudioFilter { get; init; }

    public VideoEncoder? VideoEncoder { get; init; }

    public double? FrameRate { get; init; }

    public PixelFormat? PixelFormat { get; init; }

    public int? Crf { get; init; }

    public string? VideoBitRate { get; init; }

    public EncoderPreset? Preset { get; init; }

    public string? VideoFilter { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

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

    public int? InputSampleRate { get; init; }

    public int? InputChannels { get; init; }

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
            InputChannels = options.InputChannels
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
            DropAudio = options.DropAudio,
            CopyCodecs = options.CopyCodecs,
            StartTime = options.StartTime,
            Duration = options.Duration,
            KnownDuration = options.KnownDuration,
            Progress = options.Progress,
            IoMode = options.IoMode,
            FragmentedOutput = options.FragmentedOutput,
            InputFormat = options.InputFormat
        };
}
