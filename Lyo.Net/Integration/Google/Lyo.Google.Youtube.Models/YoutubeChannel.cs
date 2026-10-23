using System.Diagnostics;

namespace Lyo.Google.Youtube.Models;

/// <summary>A YouTube channel returned by the Data API.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class YoutubeChannel
{
    public string ChannelId { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? UploadsPlaylistId { get; set; }

    public string? ThumbnailUrl { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"YoutubeChannel(ChannelId={ChannelId}, Title=\"{Title}\")";
}