using System.Globalization;
using System.Text.Json.Serialization;
using System.Xml;
using Lyo.Exceptions;
using Lyo.Google.Youtube.Models;
using Lyo.Http.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Google.Youtube.Client;

/// <summary>Data API client. Does not open a database. Callers pass returned models into their own stores.</summary>
public sealed class GoogleYoutubeClient : LyoHttpClient, IYoutubeClient
{
    private const int PageSize = 50;
    private const int MaxPages = 10;

    private readonly GoogleYoutubeClientOptions _options;

    public GoogleYoutubeClient(GoogleYoutubeClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<GoogleYoutubeClient>() ?? NullLoggerFactory.Instance.CreateLogger<GoogleYoutubeClient>(), httpClient,
            new() { PropertyNameCaseInsensitive = true }, options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public async Task<YoutubeVideo?> GetVideoAsync(string videoId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(videoId);
        var page = await GetAsAsync<VideoListWire>($"videos?part=snippet,contentDetails,status&id={Esc(videoId)}&key={Esc(_options.ApiKey)}", ct: ct).ConfigureAwait(false);
        var item = page?.Items?.FirstOrDefault();
        return item == null ? null : ToVideo(item);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<YoutubePlaylistItem>> ListChannelUploadsAsync(string channelId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(channelId);
        var page = await GetAsAsync<ChannelListWire>($"channels?part=snippet,contentDetails&id={Esc(channelId)}&key={Esc(_options.ApiKey)}", ct: ct).ConfigureAwait(false);
        var uploads = page?.Items?.FirstOrDefault()?.ContentDetails?.RelatedPlaylists?.Uploads;
        if (uploads is not { Length: > 0 })
            return [];

        return await ListPlaylistItemsAsync(uploads, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<YoutubePlaylistItem>> ListPlaylistItemsAsync(string playlistId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(playlistId);
        var items = new List<YoutubePlaylistItem>();
        string? pageToken = null;
        for (var page = 0; page < MaxPages; page++) {
            var token = pageToken is { Length: > 0 } ? $"&pageToken={Esc(pageToken)}" : "";
            var result = await GetAsAsync<PlaylistItemListWire>(
                    $"playlistItems?part=snippet,contentDetails&maxResults={PageSize}&playlistId={Esc(playlistId)}{token}&key={Esc(_options.ApiKey)}", ct: ct)
                .ConfigureAwait(false);

            if (result?.Items != null) {
                foreach (var item in result.Items) {
                    var videoId = item.ContentDetails?.VideoId ?? item.Snippet?.ResourceId?.VideoId;
                    if (videoId is not { Length: > 0 })
                        continue;

                    items.Add(
                        new() {
                            PlaylistId = playlistId,
                            VideoId = videoId,
                            Title = item.Snippet?.Title,
                            Description = item.Snippet?.Description,
                            PublishedAt = item.Snippet?.PublishedAt,
                            Position = item.Snippet?.Position ?? 0,
                            ThumbnailUrl = item.Snippet?.Thumbnails?.High?.Url ?? item.Snippet?.Thumbnails?.Default?.Url
                        });
                }
            }

            pageToken = result?.NextPageToken;
            if (string.IsNullOrEmpty(pageToken))
                break;
        }

        return items;
    }

    private static YoutubeVideo ToVideo(VideoItemWire item)
        => new() {
            VideoId = item.Id ?? string.Empty,
            ChannelId = item.Snippet?.ChannelId,
            Title = item.Snippet?.Title ?? string.Empty,
            Description = item.Snippet?.Description,
            PublishedAt = item.Snippet?.PublishedAt,
            Duration = ParseDuration(item.ContentDetails?.Duration),
            Privacy = ParsePrivacy(item.Status?.PrivacyStatus),
            LiveStatus = ParseLive(item.Snippet?.LiveBroadcastContent),
            ThumbnailUrl = item.Snippet?.Thumbnails?.High?.Url ?? item.Snippet?.Thumbnails?.Default?.Url
        };

    private static TimeSpan? ParseDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try {
            return XmlConvert.ToTimeSpan(value);
        }
        catch (FormatException) {
            return null;
        }
    }

    private static YoutubePrivacy ParsePrivacy(string? value)
        => value?.ToLower(CultureInfo.InvariantCulture) switch {
            "public" => YoutubePrivacy.Public,
            "unlisted" => YoutubePrivacy.Unlisted,
            "private" => YoutubePrivacy.Private,
            var _ => YoutubePrivacy.Unknown
        };

    private static YoutubeLiveStatus ParseLive(string? value)
        => value?.ToLower(CultureInfo.InvariantCulture) switch {
            "live" => YoutubeLiveStatus.Live,
            "upcoming" => YoutubeLiveStatus.Upcoming,
            var _ => YoutubeLiveStatus.None
        };

    private static string Esc(string value) => Uri.EscapeDataString(value);

    private sealed class VideoListWire
    {
        public List<VideoItemWire>? Items { get; set; }
    }

    private sealed class VideoItemWire
    {
        public string? Id { get; set; }

        public SnippetWire? Snippet { get; set; }

        public ContentDetailsWire? ContentDetails { get; set; }

        public StatusWire? Status { get; set; }
    }

    private sealed class ChannelListWire
    {
        public List<ChannelItemWire>? Items { get; set; }
    }

    private sealed class ChannelItemWire
    {
        public string? Id { get; set; }

        public SnippetWire? Snippet { get; set; }

        public ChannelContentWire? ContentDetails { get; set; }
    }

    private sealed class ChannelContentWire
    {
        public RelatedPlaylistsWire? RelatedPlaylists { get; set; }
    }

    private sealed class RelatedPlaylistsWire
    {
        public string? Uploads { get; set; }
    }

    private sealed class PlaylistItemListWire
    {
        public string? NextPageToken { get; set; }

        public List<PlaylistItemWire>? Items { get; set; }
    }

    private sealed class PlaylistItemWire
    {
        public SnippetWire? Snippet { get; set; }

        public ContentDetailsWire? ContentDetails { get; set; }
    }

    private sealed class SnippetWire
    {
        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateTime? PublishedAt { get; set; }

        public string? ChannelId { get; set; }

        public string? LiveBroadcastContent { get; set; }

        public int? Position { get; set; }

        public ThumbnailsWire? Thumbnails { get; set; }

        public ResourceIdWire? ResourceId { get; set; }
    }

    private sealed class ThumbnailsWire
    {
        [JsonPropertyName("default")]
        public ThumbnailWire? Default { get; set; }

        public ThumbnailWire? High { get; set; }
    }

    private sealed class ThumbnailWire
    {
        public string? Url { get; set; }
    }

    private sealed class ResourceIdWire
    {
        public string? VideoId { get; set; }
    }

    private sealed class ContentDetailsWire
    {
        public string? Duration { get; set; }

        public string? VideoId { get; set; }
    }

    private sealed class StatusWire
    {
        public string? PrivacyStatus { get; set; }
    }
}