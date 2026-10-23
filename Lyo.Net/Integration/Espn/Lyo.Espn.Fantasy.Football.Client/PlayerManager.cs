using Lyo.Espn.Fantasy.Football.Client.Builders;
using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Read APIs focused on players.</summary>
public class PlayerManager(FantasyFootballClient client)
{
    /// <summary>Loads detailed player card data for the specified ESPN player ids.</summary>
    public async Task<IReadOnlyList<PlayerInfoItemRes>?> GetInfoAsync(int leagueId, int seasonId, PlayerInfoQuery query, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetInfoAsync));
        ArgumentHelpers.ThrowIfNull(query);
        ArgumentHelpers.ThrowIfNullOrEmpty(query.PlayerIds, nameof(query.PlayerIds));
        var path = client.BuildLeaguePath(leagueId, seasonId, ["kona_playercard"]);
        var additionalPeriodIds = query.AdditionalPeriodIds.Count > 0 ? query.AdditionalPeriodIds : [$"00{seasonId}", $"10{seasonId}"];
        var filter = FantasyFilterReqBuilder.ForPlayers(query.PlayerIds).WithTopScoringPeriod(query.ScoringPeriodId, additionalPeriodIds).Build();
        var response = await client.GetLeagueViewAsync<PlayerInfoResponseRes>(path, filter, ct).ConfigureAwait(false);
        client.PlayerCache.Remember(seasonId, response?.Players);
        return response?.Players;
    }

    /// <summary>Returns cached player cards for <paramref name="playerIds" /> and calls <see cref="GetInfoAsync" /> only for the ids that are missing.</summary>
    public async Task<IReadOnlyList<PlayerInfoItemRes>> GetAsync(int leagueId, int seasonId, IEnumerable<int> playerIds, int scoringPeriodId = 18, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetAsync));
        ArgumentHelpers.ThrowIfNull(playerIds);
        var ids = playerIds.Distinct().ToArray();
        var missing = client.PlayerCache.Missing(seasonId, ids);
        if (missing.Count > 0)
            await GetInfoAsync(leagueId, seasonId, new() { PlayerIds = missing, ScoringPeriodId = scoringPeriodId }, ct).ConfigureAwait(false);

        var found = new List<PlayerInfoItemRes>(ids.Length);
        foreach (var id in ids) {
            if (client.PlayerCache.TryGet(seasonId, id, out var player) && player != null)
                found.Add(player);
        }

        return found;
    }

    /// <summary>Loads free agents and waiver-wire players for one scoring period. A successful read also fills <see cref="FantasyFootballClient.PlayerCache" />.</summary>
    public async Task<IReadOnlyList<PlayerInfoItemRes>?> GetFreeAgentsAsync(
        int leagueId,
        int seasonId,
        int scoringPeriodId,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetFreeAgentsAsync));
        var path = client.BuildLeaguePath(leagueId, seasonId, ["kona_player_info"], new Dictionary<string, string?> { ["scoringPeriodId"] = scoringPeriodId.ToString() });
        var filter = FantasyFilterReqBuilder.ForFreeAgents(limit, offset).Build();
        var response = await client.GetLeagueViewAsync<PlayerInfoResponseRes>(path, filter, ct).ConfigureAwait(false);
        client.PlayerCache.Remember(seasonId, response?.Players);
        return response?.Players;
    }

    /// <summary>Loads the fantasy player news feed from <see cref="FantasyFootballClientOptions.NewsBaseUrl" />.</summary>
    public async Task<PlayerNewsFeedRes?> GetNewsAsync(CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(client.NewsBaseUrl);
        return await client.GetAuthenticatedAsync<PlayerNewsFeedRes>(client.NewsBaseUrl.Trim(), ct).ConfigureAwait(false);
    }

    /// <summary>Returns a single player card by ESPN player id.</summary>
    public async Task<PlayerInfoItemRes?> GetPlayerAsync(int leagueId, int seasonId, int playerId, int scoringPeriodId = 18, CancellationToken ct = default)
    {
        var players = await GetInfoAsync(leagueId, seasonId, new() { PlayerIds = [playerId], ScoringPeriodId = scoringPeriodId }, ct).ConfigureAwait(false);
        return players?.FirstOrDefault();
    }
}