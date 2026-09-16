using System.Diagnostics;

namespace Lyo.Google.Youtube.Models;

/// <summary>One item in a YouTube playlist, returned by the Data API.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class YoutubePlaylistItem
{
    public string? PlaylistId { get; set; }

    public string VideoId { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTime? PublishedAt { get; set; }

    public int Position { get; set; }

    public string? ThumbnailUrl { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"YoutubePlaylistItem(VideoId={VideoId}, Position={Position})";
}
