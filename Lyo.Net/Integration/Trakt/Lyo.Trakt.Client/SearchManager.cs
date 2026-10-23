using Lyo.Exceptions;

namespace Lyo.Trakt.Client;

/// <summary>Text search and id lookup.</summary>
public sealed class SearchManager(TraktClient client)
{
    /// <summary>Searches movies with <c>GET /search/movie</c>.</summary>
    public Task<TraktPage<TraktSearchResult>> SearchMoviesAsync(string query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(query);
        var uri = "search/movie?" + client.ToQueryString(new TextQuery { Query = query });
        return client.GetPageAsync<TraktSearchResult>(uri, ct);
    }

    /// <summary>
    /// Looks up an id with <c>GET /search/{idType}/{id}</c>. IMDb ids keep the <c>tt</c> prefix. <paramref name="idType" /> is <c>trakt</c>, <c>imdb</c>, <c>tmdb</c>, or
    /// <c>tvdb</c>.
    /// </summary>
    public Task<TraktPage<TraktSearchResult>> LookupAsync(string idType, string id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(idType);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        return client.GetPageAsync<TraktSearchResult>($"search/{Uri.EscapeDataString(idType)}/{Uri.EscapeDataString(id)}", ct);
    }

    internal sealed class TextQuery
    {
        public string? Query { get; set; }
    }
}