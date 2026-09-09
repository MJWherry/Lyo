using System.Diagnostics;

namespace Lyo.Media.Models;

/// <summary>One stream from a probe JSON document.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record MediaStreamInfo
{
    /// <summary>Stream index from the prober.</summary>
    public int Index { get; init; }

    /// <summary>Stream kind, for example <c>audio</c> or <c>video</c>.</summary>
    public string? CodecType { get; init; }

    /// <summary>Codec name, for example <c>aac</c> or <c>h264</c>. Unknown names are left as strings; use <see cref="AudioEncoder.Custom" /> / <see cref="VideoEncoder.Custom" /> when you need a catalog instance.</summary>
    public string? CodecName { get; init; }

    /// <summary>Sample rate in hertz when this is an audio stream.</summary>
    public int? SampleRate { get; init; }

    /// <summary>Channel count when this is an audio stream.</summary>
    public int? Channels { get; init; }

    /// <summary>Bit rate in bits per second when reported.</summary>
    public long? BitRate { get; init; }

    /// <summary>Width in pixels when this is a video stream.</summary>
    public int? Width { get; init; }

    /// <summary>Height in pixels when this is a video stream.</summary>
    public int? Height { get; init; }

    /// <summary>Pixel format when this is a video stream, for example <c>yuv420p</c>.</summary>
    public string? PixelFormat { get; init; }

    /// <summary>Frames per second parsed from <c>avg_frame_rate</c> or <c>r_frame_rate</c>.</summary>
    public double? Fps { get; init; }

    /// <summary>True when this video stream is album art (<c>disposition.attached_pic</c>).</summary>
    public bool IsAttachedPicture { get; init; }

    /// <inheritdoc />
    public override string ToString()
        => $"MediaStreamInfo(Index={Index}, Type={CodecType}, Codec={CodecName}, {Width}x{Height}, Fps={Fps}, AttachedPic={IsAttachedPicture})";
}
