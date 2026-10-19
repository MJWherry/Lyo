using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Espn.Fantasy.Football.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FantasyFootball.Api.Tests;

public sealed class FantasyFootballInteractionsTests(FantasyFootballApiFixture fixture)
{
    private const string TopicId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";
    private const string DirectTopicId = "11111111-2222-3333-4444-555555555555";
    private const string Recipient = "{bbbb1111-2222-3333-4444-555555555555}";

    [Fact]
    public async Task Refresh_EspnPayload_PersistsGroupsMessagesAndTrades()
    {
        var handler = Handler(5201);
        var interactions = Interactions(handler);

        var league = await interactions.RefreshAsync(5201, 2026, TestCt);

        Assert.Equal("Home League", league.Name);
        Assert.Equal("Ann", Assert.Single(league.Members, member => member.EspnMemberId == "{ANN}").DisplayName);
        Assert.Equal("Alpha", Assert.Single(Assert.Single(league.Teams, team => team.EspnTeamId == 1).Roster).PlayerName);
        var conversation = Assert.Single(await interactions.ListConversationsAsync(5201, 2026, TestCt));
        Assert.Equal(TopicId, conversation.EspnTopicId);
        Assert.Equal("hey", Assert.Single(await interactions.ListMessagesAsync(5201, 2026, TopicId, TestCt)).Content);
        var trade = Assert.Single(await interactions.ListTradesAsync(5201, 2026, pending: true, ct: TestCt));
        Assert.Equal(new int?[] { 100, 200 }, trade.Items.Select(item => item.PlayerId).OrderBy(id => id).ToArray());
    }

    [Fact]
    public async Task Refresh_SuppliedCookies_SendsThemToEspn()
    {
        var handler = Handler(5207);
        var interactions = Interactions(handler);

        await interactions.RefreshAsync(5207, 2026, new FantasyFootballCredentials { EspnS2 = "pasted-s2", Swid = "{PASTED-SWID}" }, TestCt);

        Assert.NotEmpty(handler.Calls);
        Assert.All(handler.Calls, call => {
            Assert.Contains("espn_s2=pasted-s2", call.Cookie, StringComparison.Ordinal);
            Assert.Contains("SWID={PASTED-SWID}", call.Cookie, StringComparison.Ordinal);
            Assert.DoesNotContain("s2-token", call.Cookie, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Refresh_PastedCookieHeader_SplitsTokenAndSwid()
    {
        var handler = Handler(5208);
        var interactions = Interactions(handler);
        var pasted = "espn_s2=pasted-s2; SWID=bbbb1111-2222-3333-4444-555555555555";

        await interactions.RefreshAsync(5208, 2026, new FantasyFootballCredentials { EspnS2 = pasted }, TestCt);

        Assert.NotEmpty(handler.Calls);
        Assert.All(handler.Calls, call => {
            Assert.Contains("espn_s2=pasted-s2", call.Cookie, StringComparison.Ordinal);
            Assert.Contains("SWID={bbbb1111-2222-3333-4444-555555555555}", call.Cookie, StringComparison.Ordinal);
            Assert.DoesNotContain("espn_s2=espn_s2", call.Cookie, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task GetConversations_StoredLeague_DoesNotCallEspn()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>();
        await store.SaveLeagueAsync(new FantasyLeagueRecord { EspnLeagueId = 5202, SeasonId = 2026, ScoringPeriodId = 6, Members = [new FantasyMemberRecord { EspnMemberId = "{ANN}", DisplayName = "Ann" }] }, TestCt);
        await store.SaveConversationsAsync(5202, 2026, [new FantasyConversationRecord { EspnTopicId = TopicId, Type = "CHAT_ALL_MEMBERS", Members = [new FantasyMemberRecord { EspnMemberId = "{ANN}" }] }], TestCt);
        var handler = new RecordingHandler { Respond = _ => throw new InvalidOperationException("ESPN should not be called.") };
        var interactions = Interactions(handler);

        var conversations = await interactions.ListConversationsAsync(5202, 2026, TestCt);

        Assert.Equal(TopicId, Assert.Single(conversations).EspnTopicId);
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task PostMessage_TopicAndContent_PostsAndStoresUserText()
    {
        await SeedLeague(5203);
        var handler = new RecordingHandler { Respond = _ => Json(Posted("hello", TopicId)) };
        var interactions = Interactions(handler);

        var message = await interactions.PostMessageAsync(5203, 2026, TopicId, "hello", TestCt);

        Assert.Equal("hello", message.Content);
        Assert.Contains("hello", Assert.Single(handler.Calls).Body, StringComparison.Ordinal);
        Assert.Equal("hello", Assert.Single(await interactions.ListMessagesAsync(5203, 2026, TopicId, TestCt)).Content);
    }

    [Fact]
    public async Task PostDirect_RecipientAndContent_PostsToExistingDirectMessage()
    {
        await SeedLeague(5204);
        var handler = new RecordingHandler {
            Respond = call => call.Method == HttpMethod.Get
                ? Json("{\"topics\":[{\"id\":\"" + DirectTopicId + "\",\"type\":\"CHAT_DIRECT_MESSAGE\",\"isDeleted\":false,\"viewableBy\":[\"" + Recipient + "\"]}]}")
                : Json(Posted("hey", DirectTopicId))
        };
        var interactions = Interactions(handler);

        var message = await interactions.PostDirectAsync(5204, 2026, Recipient, "hey", TestCt);

        Assert.Equal("hey", message.Content);
        Assert.Contains(DirectTopicId, handler.Calls[^1].Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal("hey", Assert.Single(await interactions.ListMessagesAsync(5204, 2026, DirectTopicId, TestCt)).Content);
    }

    [Fact]
    public async Task ProposeTrade_SendAndReceive_PostsAndStoresBothSides()
    {
        await SeedLeague(5205);
        const string body = """{"id":"tx-trade","type":"TRADE_PROPOSAL","isPending":true,"scoringPeriodId":6,"items":[{"type":"TRADE","fromTeamId":1,"toTeamId":2,"playerId":100},{"type":"TRADE","fromTeamId":2,"toTeamId":1,"playerId":200}]}""";
        var handler = new RecordingHandler { Respond = _ => Json(body) };
        var interactions = Interactions(handler);

        var trade = await interactions.ProposeTradeAsync(5205, 2026, new ProposeTradeRequest { TeamId = 1, OtherTeamId = 2, ScoringPeriodId = 6, SendPlayerIds = [100], ReceivePlayerIds = [200], Comment = "deal" }, TestCt);

        using var posted = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var items = posted.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(100, items[0].GetProperty("playerId").GetInt32());
        Assert.Equal(200, items[1].GetProperty("playerId").GetInt32());
        Assert.Equal(new int?[] { 100, 200 }, trade.Items.Select(item => item.PlayerId).OrderBy(id => id).ToArray());
        Assert.Equal(2, Assert.Single(await interactions.ListTradesAsync(5205, 2026, ct: TestCt)).Items.Count);
    }

    [Fact]
    public async Task RespondTrade_Accept_PostsAcceptWithNoItems()
    {
        await SeedLeague(5206);
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-accept","type":"TRADE_ACCEPT","isPending":false,"items":[]}""") };
        var interactions = Interactions(handler);

        var trade = await interactions.RespondToTradeAsync(5206, 2026, "tx-9", new RespondTradeRequest { TeamId = 2, ScoringPeriodId = 6, Action = "accept" }, TestCt);

        using var posted = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        Assert.Equal("TRADE_ACCEPT", posted.RootElement.GetProperty("type").GetString());
        Assert.Equal("tx-9", posted.RootElement.GetProperty("relatedTransactionId").GetString());
        Assert.False(posted.RootElement.TryGetProperty("items", out _));
        Assert.Empty(trade.Items);
        Assert.Equal("TRADE_ACCEPT", Assert.Single(await interactions.ListTradesAsync(5206, 2026, ct: TestCt)).Type);
    }

    private FantasyFootballInteractions Interactions(RecordingHandler handler)
    {
        var client = new FantasyFootballClient(new FantasyFootballClientOptions { EspnS2 = "s2-token", Swid = "{SWID-1}" }, httpClient: new HttpClient(handler));
        return new FantasyFootballInteractions(client, fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>());
    }

    private async Task SeedLeague(int leagueId)
    {
        var store = fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>();
        await store.SaveLeagueAsync(new FantasyLeagueRecord {
            EspnLeagueId = leagueId,
            SeasonId = 2026,
            ScoringPeriodId = 6,
            Members = [new FantasyMemberRecord { EspnMemberId = "{ANN}", DisplayName = "Ann" }],
            Teams = [new FantasyTeamRecord { EspnTeamId = 1, Name = "Alpha" }, new FantasyTeamRecord { EspnTeamId = 2, Name = "Beta" }]
        }, TestCt);
    }

    private static RecordingHandler Handler(int leagueId)
        => new() {
            Respond = call => {
                var url = call.Uri.AbsoluteUri;
                if (url.Contains("view=mSettings", StringComparison.Ordinal))
                    return Json(LeagueJson(leagueId));
                if (url.Contains("view=mRoster", StringComparison.Ordinal))
                    return Json(RosterJson(leagueId));
                if (url.Contains("view=chat_conversations", StringComparison.Ordinal))
                    return Json($$"""{"topics":[{"id":"{{TopicId}}","type":"CHAT_ALL_MEMBERS","isDeleted":false}]}""");
                if (url.Contains("view=chat_conversation", StringComparison.Ordinal))
                    return Json(ConversationJson());
                if (url.Contains("view=mTransactions2", StringComparison.Ordinal))
                    return Json(TradeJson());
                if (url.Contains("view=mPendingTransactions", StringComparison.Ordinal))
                    return Json("""{"transactions":[]}""");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        };

    private static string LeagueJson(int leagueId)
        => "{\"id\":" + leagueId + ",\"seasonId\":2026,\"scoringPeriodId\":6,\"settings\":{\"name\":\"Home League\"},\"members\":[{\"id\":\"{ANN}\",\"displayName\":\"Ann\"}],\"teams\":[{\"id\":1,\"name\":\"Alpha\"},{\"id\":2,\"name\":\"Beta\"}]}";

    private static string RosterJson(int leagueId)
        => "{\"id\":" + leagueId + ",\"seasonId\":2026,\"scoringPeriodId\":6,\"teams\":[{\"id\":1,\"name\":\"Alpha\",\"roster\":{\"entries\":[{\"playerId\":100,\"playerPoolEntry\":{\"id\":100,\"player\":{\"id\":100,\"fullName\":\"Alpha\"}}}]}},{\"id\":2,\"name\":\"Beta\",\"roster\":{\"entries\":[]}}]}";

    private static string ConversationJson()
        => "{\"id\":\"" + TopicId + "\",\"type\":\"CHAT_ALL_MEMBERS\",\"isDeleted\":false,\"totalMessageCount\":1,\"unreadMessageCount\":0,\"messages\":[{\"id\":\"m1\",\"author\":\"{ANN}\",\"content\":\"hey\",\"date\":10,\"isDeleted\":false,\"messageTypeId\":0,\"topicId\":\"" + TopicId + "\"}]}";

    private static string TradeJson()
        => """{"transactions":[{"id":"tx-1","type":"TRADE_PROPOSAL","isPending":true,"scoringPeriodId":6,"items":[{"type":"TRADE","fromTeamId":1,"toTeamId":2,"playerId":100},{"type":"TRADE","fromTeamId":2,"toTeamId":1,"playerId":200}]}]}""";

    private static string Posted(string content, string topicId)
        => "[{\"id\":\"msg-1\",\"author\":\"{SWID-1}\",\"content\":\"" + content + "\",\"date\":1,\"isDeleted\":false,\"isEdited\":false,\"messageTypeId\":0,\"topicId\":\"" + topicId + "\"}]";

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
