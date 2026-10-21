namespace Lyo.OpenSubtitles.Client;

/// <summary>Filters for <c>GET /subtitles</c>. <see cref="ImdbId" /> may include a <c>tt</c> prefix; the client sends the numeric id.</summary>
public sealed class SubtitleSearchQuery
{
    /// <summary>IMDb title id, with or without a <c>tt</c> prefix. Leading zeros are removed.</summary>
    public string? ImdbId { get; set; }

    /// <summary>Language codes sent as one comma-separated <c>languages</c> value.</summary>
    public IReadOnlyList<string>? Languages { get; set; }
}