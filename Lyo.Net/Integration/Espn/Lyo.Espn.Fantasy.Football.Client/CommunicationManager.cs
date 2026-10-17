using Lyo.Espn.Fantasy.Football.Client.Builders;
using Lyo.Espn.Fantasy.Football.Client.Models.Request;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Reads for league chat / communication.</summary>
public class CommunicationManager(FantasyFootballClient client)
{
    /// <summary>Lists Fantasy Chat topics for a league you already loaded. Same result as <see cref="GetLeagueChatAsync(int, int, CancellationToken)" />.</summary>
    public Task<IReadOnlyList<ChatConversationRes>?> GetLeagueChatAsync(LeagueRes league, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(league);
        client.Remember(league);
        return GetLeagueChatAsync(league.Id, league.SeasonId, ct);
    }

    /// <summary>
    /// Lists Fantasy Chat topics, then loads the newest page of each conversation that is not deleted. Player cards stay empty until
    /// <see cref="LoadPlayersAsync(ChatConversationRes, int, int, CancellationToken)" />.
    /// </summary>
    public async Task<IReadOnlyList<ChatConversationRes>?> GetLeagueChatAsync(int leagueId, int seasonId, CancellationToken ct = default)
    {
        FantasyFootballClient.ValidateSeason(seasonId, nameof(GetLeagueChatAsync));
        var listed = await ListTopicsAsync(leagueId, seasonId, ct).ConfigureAwait(false);
        if (listed == null)
            return null;

        var conversations = new List<ChatConversationRes>();
        foreach (var topic in listed) {
            if (topic.IsDeleted || string.IsNullOrWhiteSpace(topic.Id))
                continue;

            var conversation = await GetConversationAsync(leagueId, seasonId, topic.Id, ct: ct).ConfigureAwait(false);
            if (conversation != null)
                conversations.Add(conversation);
        }

        return conversations;
    }

    /// <summary>Loads one conversation for a league you already have. <paramref name="topicId" /> is still the ESPN topic id.</summary>
    public Task<ChatConversationRes?> GetConversationAsync(LeagueRes league, string topicId, ChatConversationQuery? query = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(league);
        client.Remember(league);
        return GetConversationAsync(league.Id, league.SeasonId, topicId, query, ct);
    }

    /// <summary>
    /// Loads one Fantasy Chat conversation page from <c>lm-api-communication</c>. Author, members, and teams are filled from the league index when it already has them.
    /// Player cards stay empty until <see cref="LoadPlayersAsync(ChatConversationRes, int, int, CancellationToken)" />.
    /// </summary>
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
        var conversation = await client.GetCommunicationAsync<EspnChatConversation>(url, filter, ct).ConfigureAwait(false);
        return conversation == null ? null : ChatMaps.Conversation(conversation, client.LeagueDirectory);
    }

    /// <summary>Loads player cards for <paramref name="conversation" /> using a league you already have. Same result as the id-and-season overload.</summary>
    public Task LoadPlayersAsync(ChatConversationRes conversation, LeagueRes league, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(league);
        client.Remember(league);
        return LoadPlayersAsync(conversation, league.Id, league.SeasonId, ct);
    }

    /// <summary>
    /// Loads every player card <paramref name="conversation" /> names with one player request. A second call uses the cache.
    /// <see cref="ChatMessagePlayer.PlayerId" /> is left as it was.
    /// </summary>
    public async Task LoadPlayersAsync(ChatConversationRes conversation, int leagueId, int seasonId, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(conversation);
        FantasyFootballClient.ValidateSeason(seasonId, nameof(LoadPlayersAsync));
        var ids = new List<int>();
        foreach (var message in conversation.Messages ?? [])
            message.CollectPlayerIds(ids);
        if (ids.Count == 0)
            return;

        var period = client.LeagueDirectory?.ScoringPeriodId is > 0 ? client.LeagueDirectory.ScoringPeriodId : 18;
        var loaded = await client.Players.GetAsync(leagueId, seasonId, ids, period, ct).ConfigureAwait(false);
        var cards = loaded.ToDictionary(player => player.Id);
        foreach (var message in conversation.Messages ?? [])
            message.ApplyPlayers(cards);
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

    /// <summary>Posts a user text message to a league you already have. Same result as <see cref="PostMessageAsync(int, int, ChatMessageBuilder, CancellationToken)" />.</summary>
    public Task<ChatMessageRes> PostMessageAsync(LeagueRes league, ChatMessageBuilder message, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(league);
        client.Remember(league);
        return PostMessageAsync(league.Id, league.SeasonId, message, ct);
    }

    /// <summary>
    /// Posts a user text message. <see cref="ChatMessageBuilder.For" /> accepts a topic UUID or a <see cref="ChatTopics" /> value.
    /// <see cref="ChatMessageBuilder.To(string)" /> posts to the live direct message with that member. ESPN returns the new message as a one-element array; this method returns
    /// that message.
    /// Missing <see cref="FantasyFootballClientOptions.Swid" /> throws before any request is sent.
    /// </summary>
    public async Task<ChatMessageRes> PostMessageAsync(int leagueId, int seasonId, ChatMessageBuilder message, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(message);
        var swid = client.Swid;
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(swid);
        var body = message.Build(swid);
        var topicId = await ResolveTopicAsync(leagueId, seasonId, message, ct).ConfigureAwait(false);
        var url = client.BuildChatMessagePath(leagueId, seasonId, topicId);
        var posted = await client.PostAuthenticatedAsync<ChatMessageReq, EspnChatMessage[]>(url, body, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(posted == null || posted.Length == 0, "ESPN returned no chat message.");
        return ChatMaps.Message(posted![0], client.LeagueDirectory);
    }

    private async Task<string> ResolveTopicAsync(int leagueId, int seasonId, ChatMessageBuilder message, CancellationToken ct)
    {
        if (message.Recipient != null || !string.IsNullOrWhiteSpace(message.RecipientName)) {
            OperationHelpers.ThrowIf(
                message.TopicId is string topic && !string.Equals(topic, ChatTopics.DirectMessage, StringComparison.OrdinalIgnoreCase),
                "A direct-message recipient cannot be combined with another chat.");
            var memberId = ResolveMemberId(message);
            return await ResolveDirectMessageTopicAsync(leagueId, seasonId, memberId, ct).ConfigureAwait(false);
        }

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(message.TopicId);
        return await ResolveTopicIdAsync(leagueId, seasonId, message.TopicId, ct).ConfigureAwait(false);
    }

    private string ResolveMemberId(ChatMessageBuilder message)
    {
        if (message.Recipient != null) {
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(message.Recipient.Id);
            return message.Recipient.Id;
        }

        var name = message.RecipientName!;
        if (Guid.TryParse(name, out _))
            return name;

        var index = client.LeagueDirectory;
        OperationHelpers.ThrowIf(index == null, "Load the league with GetAsync or Remember before addressing a member by name.");
        return index!.FindMember(name).Id;
    }

    private async Task<string> ResolveDirectMessageTopicAsync(int leagueId, int seasonId, string memberId, CancellationToken ct)
    {
        var listed = await ListTopicsAsync(leagueId, seasonId, ct).ConfigureAwait(false);
        foreach (var topic in listed ?? []) {
            if (topic.IsDeleted || string.IsNullOrWhiteSpace(topic.Id) || !string.Equals(topic.Type, ChatTopics.DirectMessage, StringComparison.OrdinalIgnoreCase))
                continue;

            var viewable = topic.ViewableByIds;
            if (viewable.Count == 0) {
                var conversation = await GetConversationAsync(leagueId, seasonId, topic.Id, ct: ct).ConfigureAwait(false);
                viewable = conversation?.ViewableByIds;
            }

            if (viewable != null && viewable.Any(id => string.Equals(id, memberId, StringComparison.OrdinalIgnoreCase)))
                return topic.Id;
        }

        OperationHelpers.ThrowIf(true, $"No live direct message with {memberId}.");
        return "";
    }

    private async Task<IReadOnlyList<ChatConversationRes>?> ListTopicsAsync(int leagueId, int seasonId, CancellationToken ct)
    {
        var url = client.BuildCommunicationPath(leagueId, seasonId, "chat_conversations");
        var filter = new FantasyFilterReq { Topics = new() { FilterType = new() { Value = [ChatTopics.AllMembers, ChatTopics.DirectMessage] } } };
        var listed = await client.GetCommunicationAsync<EspnChatTopicList>(url, filter, ct).ConfigureAwait(false);
        if (listed == null)
            return null;

        return (listed.Topics ?? []).Select(topic => ChatMaps.Conversation(topic, client.LeagueDirectory)).ToArray();
    }

    private async Task<string> ResolveTopicIdAsync(int leagueId, int seasonId, string topicId, CancellationToken ct)
    {
        if (Guid.TryParse(topicId, out _))
            return topicId;

        var listed = await ListTopicsAsync(leagueId, seasonId, ct).ConfigureAwait(false);
        var matches = (listed ?? [])
            .Where(topic => !topic.IsDeleted && !string.IsNullOrWhiteSpace(topic.Id) && string.Equals(topic.Type, topicId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        OperationHelpers.ThrowIf(matches.Length == 0, $"No live Fantasy Chat topic of type {topicId}.");
        OperationHelpers.ThrowIf(matches.Length > 1, $"More than one live Fantasy Chat topic of type {topicId}. Pass the topic id.");
        return matches[0].Id;
    }
}
