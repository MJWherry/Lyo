using Lyo.Espn.Fantasy.Football.Client;
using Lyo.Espn.Fantasy.Football.Client.Builders;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;
using Lyo.Exceptions;

namespace Lyo.FantasyFootball.Api;

/// <summary>
/// Reads chats and trades from <see cref="IFantasyFootballStore" />. Refresh and writes call <see cref="FantasyFootballClient" />, then save what ESPN returns.
/// </summary>
public sealed class FantasyFootballInteractions(FantasyFootballClient client, IFantasyFootballStore store)
{
    /// <summary>Pulls the league, roster, chats, and trades from ESPN and upserts them.</summary>
    public Task<FantasyLeagueRecord> RefreshAsync(int leagueId, int seasonId, CancellationToken ct = default)
        => RefreshAsync(leagueId, seasonId, null, ct);

    /// <summary>Pulls the league, roster, chats, and trades from ESPN with the supplied cookies, then upserts them.</summary>
    public async Task<FantasyLeagueRecord> RefreshAsync(int leagueId, int seasonId, FantasyFootballCredentials? credentials, CancellationToken ct = default)
    {
        using var scope = Authenticate(credentials);
        var league = await client.League.GetAsync(leagueId, seasonId, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(league == null, "ESPN returned no league.");
        var period = league!.ScoringPeriodId > 0 ? league.ScoringPeriodId : 1;
        var withRosters = await client.League.GetTeamsAsync(leagueId, seasonId, period, ct).ConfigureAwait(false);
        await store.SaveLeagueAsync(ToLeague(league, withRosters), ct).ConfigureAwait(false);

        var chats = await client.Communication.GetLeagueChatAsync(leagueId, seasonId, ct).ConfigureAwait(false) ?? [];
        await store.SaveConversationsAsync(leagueId, seasonId, chats.Select(ToConversation).ToArray(), ct).ConfigureAwait(false);
        foreach (var chat in chats) {
            var messages = (chat.Messages ?? []).Select(ToMessage).ToArray();
            if (messages.Length > 0)
                await store.SaveMessagesAsync(leagueId, seasonId, chat.Id, messages, ct).ConfigureAwait(false);
        }

        var recent = await client.Transactions.GetRecentTradesAsync(leagueId, seasonId, period, ct).ConfigureAwait(false) ?? [];
        var pending = await client.Transactions.GetPendingAsync(leagueId, seasonId, ct).ConfigureAwait(false) ?? [];
        var trades = recent.Concat(pending).Where(IsTrade).GroupBy(trade => trade.Id, StringComparer.Ordinal).Select(group => ToTrade(group.Last())).ToArray();
        if (trades.Length > 0)
            await store.SaveTradesAsync(leagueId, seasonId, trades, ct).ConfigureAwait(false);

        return (await store.GetLeagueAsync(leagueId, seasonId, ct).ConfigureAwait(false))!;
    }

    /// <summary>Loads the stored league. Null before the first refresh.</summary>
    public Task<FantasyLeagueRecord?> GetLeagueAsync(int leagueId, int seasonId, CancellationToken ct = default)
        => store.GetLeagueAsync(leagueId, seasonId, ct);

    /// <summary>Lists stored chat topics. Does not call ESPN.</summary>
    public Task<IReadOnlyList<FantasyConversationRecord>> ListConversationsAsync(int leagueId, int seasonId, CancellationToken ct = default)
        => store.ListConversationsAsync(leagueId, seasonId, ct);

    /// <summary>Lists stored messages in one topic. Does not call ESPN.</summary>
    public Task<IReadOnlyList<FantasyMessageRecord>> ListMessagesAsync(int leagueId, int seasonId, string topicId, CancellationToken ct = default)
        => store.ListMessagesAsync(leagueId, seasonId, topicId, ct);

    /// <summary>Posts user text to a topic, then stores the message ESPN returns.</summary>
    public Task<FantasyMessageRecord> PostMessageAsync(int leagueId, int seasonId, string topicId, string content, CancellationToken ct = default)
        => PostMessageAsync(leagueId, seasonId, topicId, content, null, ct);

    /// <summary>Posts user text to a topic with the supplied cookies, then stores the message ESPN returns.</summary>
    public async Task<FantasyMessageRecord> PostMessageAsync(int leagueId, int seasonId, string topicId, string content, FantasyFootballCredentials? credentials, CancellationToken ct = default)
    {
        using var scope = Authenticate(credentials);
        var posted = await client.Communication.PostMessageAsync(leagueId, seasonId, new ChatMessageBuilder(topicId).Content(content), ct).ConfigureAwait(false);
        var message = ToMessage(posted);
        await store.SaveMessagesAsync(leagueId, seasonId, string.IsNullOrWhiteSpace(posted.TopicId) ? topicId : posted.TopicId, [message], ct).ConfigureAwait(false);
        return message;
    }

    /// <summary>Posts user text to an existing direct message, then stores the message ESPN returns.</summary>
    public Task<FantasyMessageRecord> PostDirectAsync(int leagueId, int seasonId, string recipient, string content, CancellationToken ct = default)
        => PostDirectAsync(leagueId, seasonId, recipient, content, null, ct);

    /// <summary>Posts user text to an existing direct message with the supplied cookies, then stores the message ESPN returns.</summary>
    public async Task<FantasyMessageRecord> PostDirectAsync(int leagueId, int seasonId, string recipient, string content, FantasyFootballCredentials? credentials, CancellationToken ct = default)
    {
        using var scope = Authenticate(credentials);
        var posted = await client.Communication.PostMessageAsync(leagueId, seasonId, ChatMessageBuilder.To(recipient).Content(content), ct).ConfigureAwait(false);
        OperationHelpers.ThrowIf(string.IsNullOrWhiteSpace(posted.TopicId), "ESPN returned a direct message with no topic.");
        var message = ToMessage(posted);
        await store.SaveMessagesAsync(leagueId, seasonId, posted.TopicId!, [message], ct).ConfigureAwait(false);
        return message;
    }

    /// <summary>Lists stored trades. <paramref name="pending" /> null returns every trade.</summary>
    public Task<IReadOnlyList<FantasyTradeRecord>> ListTradesAsync(int leagueId, int seasonId, bool? pending = null, CancellationToken ct = default)
        => store.ListTradesAsync(leagueId, seasonId, pending, ct);

    /// <summary>Proposes a trade, then stores the transaction ESPN returns.</summary>
    public Task<FantasyTradeRecord> ProposeTradeAsync(int leagueId, int seasonId, ProposeTradeRequest request, CancellationToken ct = default)
        => ProposeTradeAsync(leagueId, seasonId, request, null, ct);

    /// <summary>Proposes a trade with the supplied cookies, then stores the transaction ESPN returns.</summary>
    public async Task<FantasyTradeRecord> ProposeTradeAsync(int leagueId, int seasonId, ProposeTradeRequest request, FantasyFootballCredentials? credentials, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        using var scope = Authenticate(credentials);
        var builder = new TradeTransactionBuilder(request.TeamId, request.OtherTeamId, request.ScoringPeriodId).Comment(request.Comment);
        foreach (var playerId in request.SendPlayerIds)
            builder.Send(playerId);
        foreach (var playerId in request.ReceivePlayerIds)
            builder.Receive(playerId);

        var posted = await client.Transactions.ProposeTradeAsync(leagueId, seasonId, builder, ct).ConfigureAwait(false);
        var trade = ToTrade(posted);
        await store.SaveTradesAsync(leagueId, seasonId, [trade], ct).ConfigureAwait(false);
        return trade;
    }

    /// <summary>Accepts, declines, upholds, or vetoes a trade, then stores the transaction ESPN returns.</summary>
    public Task<FantasyTradeRecord> RespondToTradeAsync(int leagueId, int seasonId, string transactionId, RespondTradeRequest request, CancellationToken ct = default)
        => RespondToTradeAsync(leagueId, seasonId, transactionId, request, null, ct);

    /// <summary>Accepts, declines, upholds, or vetoes a trade with the supplied cookies, then stores the transaction ESPN returns.</summary>
    public async Task<FantasyTradeRecord> RespondToTradeAsync(int leagueId, int seasonId, string transactionId, RespondTradeRequest request, FantasyFootballCredentials? credentials, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(transactionId);
        using var scope = Authenticate(credentials);
        var builder = request.Action.Trim().ToLowerInvariant() switch {
            "accept" => TradeTransactionBuilder.Accept(request.TeamId, request.ScoringPeriodId, transactionId),
            "decline" => TradeTransactionBuilder.Decline(request.TeamId, request.ScoringPeriodId, transactionId),
            "uphold" => TradeTransactionBuilder.Uphold(request.TeamId, request.ScoringPeriodId, transactionId),
            "veto" => TradeTransactionBuilder.Veto(request.TeamId, request.ScoringPeriodId, transactionId),
            _ => throw new ArgumentException("Action must be accept, decline, uphold, or veto.", nameof(request))
        };
        builder.Comment(request.Comment);
        var posted = await client.Transactions.RespondToTradeAsync(leagueId, seasonId, builder, ct).ConfigureAwait(false);
        var trade = ToTrade(posted);
        await store.SaveTradesAsync(leagueId, seasonId, [trade], ct).ConfigureAwait(false);
        return trade;
    }

    private IDisposable Authenticate(FantasyFootballCredentials? credentials)
    {
        credentials = FantasyFootballCredentials.Normalize(credentials);
        if (credentials == null)
            return NoopScope.Instance;

        return client.UseCredentials(credentials.EspnS2, credentials.Swid);
    }

    private sealed class NoopScope : IDisposable
    {
        public static readonly NoopScope Instance = new();

        public void Dispose() { }
    }

    private static bool IsTrade(TransactionRes trade)
        => trade.Type?.StartsWith("TRADE", StringComparison.OrdinalIgnoreCase) == true
            || trade.Items.Any(item => string.Equals(item.Type, "TRADE", StringComparison.OrdinalIgnoreCase));

    private static FantasyLeagueRecord ToLeague(LeagueRes league, LeagueRes? withRosters)
        => new() {
            EspnLeagueId = league.Id,
            SeasonId = league.SeasonId,
            Name = league.Settings?.Name,
            ScoringPeriodId = league.ScoringPeriodId,
            Members = (league.Members ?? []).Select(ToMember).ToArray(),
            Teams = (withRosters?.Teams ?? league.Teams ?? []).Select(ToTeam).ToArray()
        };

    private static FantasyMemberRecord ToMember(MemberRes member)
        => new() { EspnMemberId = member.Id, DisplayName = member.DisplayName, FirstName = member.FirstName, LastName = member.LastName };

    private static FantasyTeamRecord ToTeam(TeamRes team)
        => new() {
            EspnTeamId = team.Id,
            Name = team.Name,
            Abbrev = team.Abbrev,
            Location = team.Location,
            Nickname = team.Nickname,
            PrimaryOwnerId = team.PrimaryOwner,
            Roster = (team.Roster?.Entries ?? []).Where(entry => entry.PlayerId != null).Select(entry => new FantasyRosterEntryRecord {
                ScoringPeriodId = 0,
                PlayerId = entry.PlayerId!.Value,
                PlayerName = entry.PlayerPoolEntry?.Player?.FullName
            }).ToArray()
        };

    private static FantasyConversationRecord ToConversation(ChatConversationRes conversation)
    {
        var members = conversation.Members.Count > 0
            ? conversation.Members.Select(ToMember).ToArray()
            : conversation.ViewableByIds.Select(id => new FantasyMemberRecord { EspnMemberId = id }).ToArray();
        return new FantasyConversationRecord {
            EspnTopicId = conversation.Id,
            Type = conversation.Type,
            UnreadCount = conversation.UnreadMessageCount,
            TotalMessageCount = conversation.TotalMessageCount,
            Subject = conversation.Subject,
            Members = members
        };
    }

    private static FantasyMessageRecord ToMessage(ChatMessageRes message)
        => new() {
            EspnMessageId = message.Id,
            AuthorEspnId = message.AuthorId,
            AuthorName = message.Author?.DisplayName,
            Kind = message.Kind.ToString(),
            Content = message.Content,
            SentAt = message.Date,
            EspnTransactionId = message.TransactionId,
            Players = Players(message).ToArray()
        };

    private static IEnumerable<FantasyMessagePlayerRecord> Players(ChatMessageRes message)
    {
        foreach (var player in Side(message.Side1, 1))
            yield return player;
        foreach (var player in Side(message.Side2, 2))
            yield return player;
    }

    private static IEnumerable<FantasyMessagePlayerRecord> Side(ChatMessageSide? side, int number)
    {
        if (side == null)
            yield break;

        foreach (var player in side.Added)
            yield return Player(side, number, player, "added");
        foreach (var player in side.Dropped)
            yield return Player(side, number, player, "dropped");
    }

    private static FantasyMessagePlayerRecord Player(ChatMessageSide side, int number, ChatMessagePlayer player, string direction)
        => new() {
            Side = number,
            EspnTeamId = side.TeamId,
            PlayerId = player.PlayerId,
            PlayerName = player.Player?.Player?.FullName,
            Direction = direction
        };

    private static FantasyTradeRecord ToTrade(TransactionRes trade)
        => new() {
            EspnTransactionId = trade.Id,
            Type = trade.Type,
            Status = trade.Status,
            ScoringPeriodId = trade.ScoringPeriodId,
            IsPending = trade.IsPending == true,
            Items = trade.Items.Select(item => new FantasyTradeItemRecord {
                FromEspnTeamId = item.FromTeamId,
                ToEspnTeamId = item.ToTeamId,
                PlayerId = item.PlayerId,
                PlayerName = item.Player?.FullName
            }).ToArray()
        };
}
