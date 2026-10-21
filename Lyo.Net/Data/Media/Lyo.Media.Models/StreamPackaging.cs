namespace Lyo.Media.Models;

/// <summary>HLS or DASH segment settings. The output path is the playlist.</summary>
public sealed record StreamPackaging
{
    /// <summary>Segment length. Null means the factory default.</summary>
    public TimeSpan? SegmentDuration { get; init; }

    /// <summary>HLS playlist type. Ignored for DASH.</summary>
    public HlsPlaylistType? PlaylistType { get; init; }

    /// <summary>Segment filename or pattern. Null means a name beside the playlist.</summary>
    public string? SegmentFileName { get; init; }

    /// <summary>True when this packaging is DASH. False means HLS.</summary>
    public bool IsDash { get; init; }

    /// <summary>HLS packaging. Default segment length is 6 seconds and the playlist type is <see cref="HlsPlaylistType.Vod" />.</summary>
    public static StreamPackaging Hls(TimeSpan? segmentDuration = null, HlsPlaylistType? playlistType = null, string? segmentFileName = null)
        => new() { SegmentDuration = segmentDuration ?? TimeSpan.FromSeconds(6), PlaylistType = playlistType ?? HlsPlaylistType.Vod, SegmentFileName = segmentFileName };

    /// <summary>DASH packaging. Default segment length is 4 seconds.</summary>
    public static StreamPackaging Dash(TimeSpan? segmentDuration = null, string? segmentFileName = null)
        => new() { SegmentDuration = segmentDuration ?? TimeSpan.FromSeconds(4), SegmentFileName = segmentFileName, IsDash = true };
}