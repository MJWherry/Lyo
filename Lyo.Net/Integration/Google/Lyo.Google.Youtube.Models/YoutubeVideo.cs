using System.Diagnostics;

namespace Lyo.Google.Youtube.Models;

/// <summary>A YouTube video returned by the Data API. Not a catalog column.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class YoutubeVideo
{
    public string VideoId { get; set; } = string.Empty;

    public string? ChannelId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? PublishedAt { get; set; }

    public TimeSpan? Duration { get; set; }

    public YoutubePrivacy Privacy { get; set; }

    public YoutubeLiveStatus LiveStatus { get; set; }

    public string? ThumbnailUrl { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"YoutubeVideo(VideoId={VideoId}, Title=\"{Title}\")";
}