using Lyo.Google.Youtube.Models;

namespace Lyo.Google.Youtube.Client;

/// <summary>YouTube Data API reads. Returns models from <c>Lyo.Google.Youtube.Models</c>.</summary>
public interface IYoutubeClient
{
    /// <summary>Loads one video by id. Returns null when YouTube has no matching item.</summary>
    Task<YoutubeVideo?> GetVideoAsync(string videoId, CancellationToken ct = default);

    /// <summary>Lists videos in the channel uploads playlist.</summary>
    Task<IReadOnlyList<YoutubePlaylistItem>> ListChannelUploadsAsync(string channelId, CancellationToken ct = default);

    /// <summary>Lists items in a playlist, ordered as YouTube returns them.</summary>
    Task<IReadOnlyList<YoutubePlaylistItem>> ListPlaylistItemsAsync(string playlistId, CancellationToken ct = default);
}
