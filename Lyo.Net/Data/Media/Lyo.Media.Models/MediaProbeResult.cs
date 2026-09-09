using System.Diagnostics;

namespace Lyo.Media.Models;

/// <summary>
/// Metadata returned after probing an audio or video file. Muxed files list every stream.
/// <see cref="HasVideo" /> ignores album art. Each prober flattens the first real audio vs first real video stream.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MediaProbeResult
{
    /// <summary>Path of the file that was probed. Null when the source was a stream or byte array.</summary>
    public string? FilePath { get; init; }

    /// <summary>Duration in seconds.</summary>
    public double? DurationSeconds { get; init; }

    /// <summary>Container name, for example <c>wav</c>, <c>mp3</c>, or <c>mov,mp4,m4a,3gp,3g2,mj2</c>.</summary>
    public string? Format { get; init; }

    /// <summary>Sample rate of the first audio stream, in hertz.</summary>
    public int? SampleRate { get; init; }

    /// <summary>Channel count of the first audio stream.</summary>
    public int? Channels { get; init; }

    /// <summary>Codec of the first audio stream.</summary>
    public string? Codec { get; init; }

    /// <summary>Container bit rate in bits per second.</summary>
    public long? BitRate { get; init; }

    /// <summary>File size in bytes when reported.</summary>
    public long? FileSizeBytes { get; init; }

    /// <summary>True when a video stream exists that is not album art.</summary>
    public bool HasVideo { get; init; }

    /// <summary>True when an audio stream exists.</summary>
    public bool HasAudio { get; init; }

    /// <summary>Codec of the first real video stream.</summary>
    public string? VideoCodec { get; init; }

    /// <summary>Width of the first real video stream, in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>Height of the first real video stream, in pixels.</summary>
    public int? Height { get; init; }

    /// <summary>Frames per second of the first real video stream.</summary>
    public double? Fps { get; init; }

    /// <summary>Format tags from the probe (title, artist, and similar).</summary>
    public IReadOnlyDictionary<string, string>? RawMetadata { get; init; }

    /// <summary>Every stream the probe listed.</summary>
    public IReadOnlyList<MediaStreamInfo> Streams { get; init; } = [];

    /// <summary>First audio stream, or null.</summary>
    public MediaStreamInfo? FirstAudio => Streams.FirstOrDefault(s => string.Equals(s.CodecType, "audio", StringComparison.OrdinalIgnoreCase));

    /// <summary>First real video stream (not album art), or null.</summary>
    public MediaStreamInfo? FirstVideo
        => Streams.FirstOrDefault(s => string.Equals(s.CodecType, "video", StringComparison.OrdinalIgnoreCase) && !s.IsAttachedPicture);

    /// <inheritdoc />
    public override string ToString()
        => $"Probe: {FilePath ?? "(stream)"} (Duration={DurationSeconds}s, Format={Format}, Audio={Codec} {SampleRate}Hz {Channels}ch, Video={VideoCodec} {Width}x{Height} @{Fps}fps)";
}
