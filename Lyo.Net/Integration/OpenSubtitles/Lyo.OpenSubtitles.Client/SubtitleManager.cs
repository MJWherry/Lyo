using Lyo.Exceptions;

namespace Lyo.OpenSubtitles.Client;

/// <summary>Subtitle search and download.</summary>
public sealed class SubtitleManager(OpenSubtitlesClient client)
{
    /// <summary>
    /// Searches subtitles. When <paramref name="session" /> is set, the call uses that login hostname. <see cref="SubtitleSearchQuery.ImdbId" /> is sent as a number without a
    /// <c>tt</c> prefix.
    /// </summary>
    public Task<SubtitleSearchResponse?> SearchAsync(SubtitleSearchQuery query, OpenSubtitlesSession? session = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        var wire = new SearchWire { ImdbId = NormalizeImdb(query.ImdbId), Languages = JoinLanguages(query.Languages) };
        var uri = session == null ? "subtitles" : client.Url(session, "subtitles");
        return client.GetAsAsync<SearchWire, SubtitleSearchResponse>(uri, wire, ct: ct);
    }

    /// <summary>Asks for a temporary download link. Does not fetch the file. Requires a login session.</summary>
    public Task<SubtitleDownload> DownloadAsync(int fileId, OpenSubtitlesSession session, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(session);
        return client.PostAsAsync<DownloadRequest, SubtitleDownload>(client.Url(session, "download"), new() { FileId = fileId }, OpenSubtitlesClient.Bearer(session), ct);
    }

    private static int? NormalizeImdb(string? imdbId)
    {
        if (imdbId == null || string.IsNullOrWhiteSpace(imdbId))
            return null;

        var value = imdbId.Trim();
        if (value.StartsWith("tt", StringComparison.OrdinalIgnoreCase))
            value = value[2..];

        value = value.TrimStart('0');
        if (value.Length == 0)
            return 0;

        if (!int.TryParse(value, out var id))
            throw new ArgumentException("IMDb id must be numeric, with an optional tt prefix.", nameof(imdbId));

        return id;
    }

    private static string? JoinLanguages(IReadOnlyList<string>? languages) => languages is { Count: > 0 } ? string.Join(",", languages) : null;

    internal sealed class SearchWire
    {
        public int? ImdbId { get; set; }

        public string? Languages { get; set; }
    }

    internal sealed class DownloadRequest
    {
        public int FileId { get; set; }
    }
}