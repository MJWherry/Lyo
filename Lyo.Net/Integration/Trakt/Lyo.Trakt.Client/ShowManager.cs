using Lyo.Exceptions;

namespace Lyo.Trakt.Client;

/// <summary>Show catalog calls on <c>https://api.trakt.tv</c>.</summary>
public sealed class ShowManager(TraktClient client)
{
    /// <summary>Loads the episodes in one season. <c>GET /shows/{id}/seasons/{season}</c> returns an array, not a season object.</summary>
    public async Task<IReadOnlyList<TraktEpisode>> GetSeasonAsync(string showId, int season, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(showId);
        var episodes = await client.GetAsAsync<List<TraktEpisode>>($"shows/{Uri.EscapeDataString(showId)}/seasons/{season}", ct: ct).ConfigureAwait(false);
        return episodes ?? [];
    }
}
