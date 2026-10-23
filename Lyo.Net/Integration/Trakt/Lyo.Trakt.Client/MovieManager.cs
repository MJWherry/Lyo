using Lyo.Exceptions;

namespace Lyo.Trakt.Client;

/// <summary>Movie catalog calls on <c>https://api.trakt.tv</c>.</summary>
public sealed class MovieManager(TraktClient client)
{
    /// <summary>Loads one movie. <paramref name="id" /> may be a Trakt id, slug, or <c>tt</c> IMDb id.</summary>
    public Task<TraktMovie?> GetAsync(string id, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(id);
        return client.GetAsAsync<TraktMovie>($"movies/{Uri.EscapeDataString(id)}", ct: ct);
    }

    /// <summary>Returns trending movies. Each row is a watcher count plus the movie.</summary>
    public Task<TraktPage<TraktTrendingMovie>> TrendingAsync(CancellationToken ct = default) => client.GetPageAsync<TraktTrendingMovie>("movies/trending", ct);
}