using Lyo.Espn.Fantasy.Football.Builders;
using Lyo.Espn.Fantasy.Football.Models.Request;
using Lyo.Espn.Fantasy.Football.Models.Response;

namespace Lyo.Espn.Fantasy.Football;

/// <summary>League chat and communication reads.</summary>
public class CommunicationManager(FantasyFootballClient client)
{
    /// <summary>Gets the league message board grouped by topic type.</summary>
    public async Task<LeagueChatRes?> GetLeagueChatAsync(int leagueId, int seasonId, LeagueChatQuery? query = null, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetLeagueChatAsync));
        query ??= new();
        FantasyFilterReq? filter = null;
        if (query.TopicTypes.Count > 0)
            filter = FantasyFilterReqBuilder.ForTopicTypes(query.TopicTypes).Build();

        var path = client.BuildLeaguePath(leagueId, seasonId, ["kona_league_messageboard"], suffix: "/communication");
        return await client.GetLeagueViewAsync<LeagueChatRes>(path, filter, ct).ConfigureAwait(false);
    }

    /// <summary>Gets recent league activity such as adds, drops, and trades.</summary>
    public async Task<IReadOnlyList<CommunicationTopicRes>?> GetRecentActivityAsync(int leagueId, int seasonId, RecentActivityQuery? query = null, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetRecentActivityAsync));
        query ??= new();
        var filter = FantasyFilterReqBuilder.ForRecentActivity(query.Limit, query.Offset, query.LimitPerMessageSet, query.MessageTypeIds).Build();
        var path = client.BuildLeaguePath(leagueId, seasonId, ["kona_league_communication"], suffix: "/communication");
        var response = await client.GetLeagueViewAsync<RecentActivityResponseRes>(path, filter, ct).ConfigureAwait(false);
        return response?.Topics;
    }
}