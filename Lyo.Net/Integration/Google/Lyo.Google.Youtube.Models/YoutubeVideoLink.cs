using System.Diagnostics;

namespace Lyo.Google.Youtube.Models;

/// <summary>Stable YouTube ids stored on a catalog row. Live fields stay on the Data API response types.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class YoutubeVideoLink
{
    public string VideoId { get; set; } = string.Empty;

    public string ChannelId { get; set; } = string.Empty;

    public string? PlaylistId { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"YoutubeVideoLink(VideoId={VideoId}, ChannelId={ChannelId})";
}