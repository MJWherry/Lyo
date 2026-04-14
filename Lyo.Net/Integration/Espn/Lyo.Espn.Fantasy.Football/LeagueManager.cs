using Lyo.Espn.Fantasy.Football.Models.Response;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>League-oriented read operations.</summary>
public class LeagueManager(FantasyFootballClient client)
{
    /// <summary>Gets league settings, members, and teams for a season.</summary>
    public async Task<LeagueRes?> GetAsync(int leagueId, int seasonId, CancellationToken ct = default)
    {
        var path = client.BuildLeaguePath(leagueId, seasonId, ["mSettings", "mTeam", "mStandings"]);
        return await client.GetLeagueViewAsync<LeagueRes>(path, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Gets teams and rosters for a specific scoring period.</summary>
    public async Task<LeagueRes?> GetTeamsAsync(int leagueId, int seasonId, int scoringPeriodId, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetTeamsAsync));
        var path = client.BuildLeaguePath(leagueId, seasonId, ["mRoster", "mTeam"], new Dictionary<string, string?> { ["scoringPeriodId"] = scoringPeriodId.ToString() });
        return await client.GetLeagueViewAsync<LeagueRes>(path, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Gets a single team with roster info for a scoring period.</summary>
    public async Task<TeamRes?> GetTeamAsync(int leagueId, int seasonId, int scoringPeriodId, int teamId, CancellationToken ct = default)
    {
        var league = await GetTeamsAsync(leagueId, seasonId, scoringPeriodId, ct).ConfigureAwait(false);
        return league?.Teams?.FirstOrDefault(i => i.Id == teamId);
    }

    /// <summary>Gets draft metadata and picks for a season.</summary>
    public async Task<DraftResponseRes?> GetDraftAsync(int leagueId, int seasonId, int scoringPeriodId = 0, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetDraftAsync));
        var path = client.BuildLeaguePath(leagueId, seasonId, ["mDraftDetail"], new Dictionary<string, string?> { ["scoringPeriodId"] = scoringPeriodId.ToString() });
        return await client.GetLeagueViewAsync<DraftResponseRes>(path, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Gets matchup and scoreboard data for a scoring period.</summary>
    public async Task<IReadOnlyList<MatchupRes>?> GetMatchupsAsync(int leagueId, int seasonId, int scoringPeriodId, int? matchupPeriodId = null, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetMatchupsAsync));
        var path = client.BuildLeaguePath(
            leagueId, seasonId, ["mMatchup", "mMatchupScore", "mScoreboard"], new Dictionary<string, string?> { ["scoringPeriodId"] = scoringPeriodId.ToString() });

        var response = await client.GetLeagueViewAsync<MatchupResponseRes>(path, ct: ct).ConfigureAwait(false);
        if (response is null)
            return null;

        return matchupPeriodId == null ? response.Schedule : response.Schedule.Where(i => i.MatchupPeriodId == matchupPeriodId).ToArray();
    }
}