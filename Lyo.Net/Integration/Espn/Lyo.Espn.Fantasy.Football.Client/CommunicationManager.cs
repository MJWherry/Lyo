using Lyo.Espn.Fantasy.Football.Client.Builders;
using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Reads for league chat / communication.</summary>
public class CommunicationManager(FantasyFootballClient client)
{
    /// <summary>Lists Fantasy Chat topics, then loads the newest page of each conversation that is not deleted.</summary>
    public async Task<IReadOnlyList<ChatConversationRes>?> GetLeagueChatAsync(int leagueId, int seasonId, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetLeagueChatAsync));
        var url = client.BuildCommunicationPath(leagueId, seasonId, "chat_conversations");
        var filter = new FantasyFilterReq { Topics = new() { FilterType = new() { Value = ["CHAT_ALL_MEMBERS", "CHAT_DIRECT_MESSAGE"] } } };
        var listed = await client.GetCommunicationAsync<ChatConversationsRes>(url, filter, ct).ConfigureAwait(false);
        if (listed == null)
            return null;

        var conversations = new List<ChatConversationRes>();
        foreach (var topic in listed.Topics ?? []) {
            if (topic.IsDeleted || string.IsNullOrWhiteSpace(topic.Id))
                continue;

            var conversation = await GetConversationAsync(leagueId, seasonId, topic.Id, ct: ct).ConfigureAwait(false);
            if (conversation != null)
                conversations.Add(conversation);
        }

        return conversations;
    }

    /// <summary>Loads one Fantasy Chat conversation page from <c>lm-api-communication</c>.</summary>
    public async Task<ChatConversationRes?> GetConversationAsync(
        int leagueId, int seasonId, string topicId, ChatConversationQuery? query = null, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetConversationAsync));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(topicId);
        query ??= new();
        ArgumentHelpers.ThrowIfNegativeOrZero(query.Limit);
        ArgumentHelpers.ThrowIfNegative(query.Offset);
        var url = client.BuildCommunicationPath(leagueId, seasonId, "chat_conversation", topicId);
        var filter = new ChatMessagePageReq { Limit = query.Limit, Offset = query.Offset, SortDate = new() { SortPriority = 1, SortAsc = query.SortAscending } };
        return await client.GetCommunicationAsync<ChatConversationRes>(url, filter, ct).ConfigureAwait(false);
    }

    /// <summary>Loads recent league activity such as adds, drops, and trades.</summary>
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
