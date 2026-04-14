using Lyo.Espn.Fantasy.Football.Builders;
using Lyo.Espn.Fantasy.Football.Models.Request;
using Lyo.Espn.Fantasy.Football.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>Player-oriented read operations.</summary>
public class PlayerManager(FantasyFootballClient client)
{
    /// <summary>Gets detailed player card data for the specified ESPN player ids.</summary>
    public async Task<IReadOnlyList<PlayerInfoItemRes>?> GetInfoAsync(int leagueId, int seasonId, PlayerInfoQuery query, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetInfoAsync));
        ArgumentHelpers.ThrowIfNull(query, nameof(query));
        ArgumentHelpers.ThrowIfNullOrEmpty(query.PlayerIds, nameof(query.PlayerIds));
        var path = client.BuildLeaguePath(leagueId, seasonId, ["kona_playercard"]);
        var additionalPeriodIds = query.AdditionalPeriodIds.Count > 0 ? query.AdditionalPeriodIds : [$"00{seasonId}", $"10{seasonId}"];
        var filter = FantasyFilterReqBuilder.ForPlayers(query.PlayerIds).WithTopScoringPeriod(query.ScoringPeriodId, additionalPeriodIds).Build();
        var response = await client.GetLeagueViewAsync<PlayerInfoResponseRes>(path, filter, ct).ConfigureAwait(false);
        return response?.Players;
    }

    /// <summary>Gets a single player card by ESPN player id.</summary>
    public async Task<PlayerInfoItemRes?> GetPlayerAsync(int leagueId, int seasonId, int playerId, int scoringPeriodId = 18, CancellationToken ct = default)
    {
        var players = await GetInfoAsync(leagueId, seasonId, new() { PlayerIds = [playerId], ScoringPeriodId = scoringPeriodId }, ct).ConfigureAwait(false);
        return players?.FirstOrDefault();
    }
}