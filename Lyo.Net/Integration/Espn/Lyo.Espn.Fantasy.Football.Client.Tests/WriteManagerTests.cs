using System.Net;
using System.Text;
using System.Text.Json;
using Lyo.Espn.Fantasy.Football.Client.Builders;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;

namespace Lyo.Espn.Fantasy.Football.Client.Tests;

public sealed class WriteManagerTests
{
    private const int LeagueId = 1001;
    private const int SeasonId = 2026;
    private const string TopicId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";

    private const string WritesUrl = "https://lm-api-writes.fantasy.espn.com/apis/v3/games/ffl/seasons/2026/segments/0/leagues/1001/transactions/";

    private const string MessageUrl =
        "https://lm-api-communication.fantasy.espn.com/apis/v3/games/ffl/seasons/2026/segments/0/leagues/1001/communication/topics/622adbe6-5001-33d1-82b0-d9d70c0cefcd/messages/?source=%7BSWID-1%7D";

    private const string PostedMessageJson =
        """[{"id":"msg-1","author":"{SWID-1}","content":"hello","date":1,"isDeleted":false,"isEdited":false,"messageTypeId":0,"topicId":"622adbe6-5001-33d1-82b0-d9d70c0cefcd"}]""";

    [Fact]
    public void LineupTransactionBuilder_NoMoves_Throws() => Assert.Throws<InvalidOperationException>(() => new LineupTransactionBuilder(12, 6).Build());

    [Fact]
    public void TradeTransactionBuilder_NoPlayers_Throws() => Assert.Throws<InvalidOperationException>(() => new TradeTransactionBuilder(12, 10, 6).Build());

    [Fact]
    public void ChatMessageBuilder_BlankContent_Throws() => Assert.Throws<InvalidOperationException>(() => new ChatMessageBuilder(TopicId).Content("  ").Build("{SWID-1}"));

    [Fact]
    public async Task SetLineupAsync_PostsRosterLineupMoves()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-lineup","items":[]}""") };
        var client = Client(handler);
        var lineup = new LineupTransactionBuilder(12, 6).Move(111, 4, 2);
        await client.Transactions.SetLineupAsync(LeagueId, SeasonId, lineup, TestCt);
        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal(WritesUrl, call.Uri.AbsoluteUri);
        Assert.Contains("SWID={SWID-1}", call.Headers["Cookie"]);
        using var body = JsonDocument.Parse(call.Body!);
        var root = body.RootElement;
        Assert.False(root.GetProperty("isLeagueManager").GetBoolean());
        Assert.Equal(12, root.GetProperty("teamId").GetInt32());
        Assert.Equal("ROSTER", root.GetProperty("type").GetString());
        Assert.Equal(6, root.GetProperty("scoringPeriodId").GetInt32());
        Assert.Equal("EXECUTE", root.GetProperty("executionType").GetString());
        Assert.Equal("{SWID-1}", root.GetProperty("memberId").GetString());
        var item = Assert.Single(root.GetProperty("items").EnumerateArray());
        Assert.Equal(111, item.GetProperty("playerId").GetInt32());
        Assert.Equal("LINEUP", item.GetProperty("type").GetString());
        Assert.Equal(4, item.GetProperty("fromLineupSlotId").GetInt32());
        Assert.Equal(2, item.GetProperty("toLineupSlotId").GetInt32());
        Assert.False(item.TryGetProperty("fromTeamId", out var _));
        Assert.False(item.TryGetProperty("toTeamId", out var _));
    }

    [Fact]
    public async Task AddDropAsync_FreeAgent_PostsAddAndDrop()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-fa","items":[]}""") };
        var client = Client(handler);
        var roster = RosterTransactionBuilder.FreeAgent(12, 6).Add(5).Drop(6);
        await client.Transactions.AddDropAsync(LeagueId, SeasonId, roster, TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var root = body.RootElement;
        Assert.Equal(WritesUrl, handler.Calls[0].Uri.AbsoluteUri);
        Assert.Equal("FREEAGENT", root.GetProperty("type").GetString());
        Assert.Equal("EXECUTE", root.GetProperty("executionType").GetString());
        Assert.False(root.TryGetProperty("bidAmount", out var _));
        var items = root.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);
        Assert.Equal("ADD", items[0].GetProperty("type").GetString());
        Assert.Equal(5, items[0].GetProperty("playerId").GetInt32());
        Assert.Equal(12, items[0].GetProperty("toTeamId").GetInt32());
        Assert.False(items[0].TryGetProperty("fromTeamId", out var _));
        Assert.Equal("DROP", items[1].GetProperty("type").GetString());
        Assert.Equal(6, items[1].GetProperty("playerId").GetInt32());
        Assert.Equal(12, items[1].GetProperty("fromTeamId").GetInt32());
        Assert.False(items[1].TryGetProperty("toTeamId", out var _));
    }

    [Fact]
    public async Task AddDropAsync_Waiver_PostsBid()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-waiver","items":[]}""") };
        var client = Client(handler);
        await client.Transactions.AddDropAsync(LeagueId, SeasonId, RosterTransactionBuilder.Waiver(12, 6, 7).Add(5), TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var root = body.RootElement;
        Assert.Equal("WAIVER", root.GetProperty("type").GetString());
        Assert.Equal(7, root.GetProperty("bidAmount").GetInt32());
        Assert.Equal("ADD", Assert.Single(root.GetProperty("items").EnumerateArray()).GetProperty("type").GetString());
    }

    [Fact]
    public async Task CancelWaiverAsync_PostsCancel()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-cancel","items":[]}""") };
        var client = Client(handler);
        await client.Transactions.CancelWaiverAsync(LeagueId, SeasonId, RosterTransactionBuilder.CancelWaiver(12, 6, "waiver-1"), TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var root = body.RootElement;
        Assert.Equal("WAIVER", root.GetProperty("type").GetString());
        Assert.Equal("CANCEL", root.GetProperty("executionType").GetString());
        Assert.Equal("waiver-1", root.GetProperty("relatedTransactionId").GetString());
        Assert.False(root.TryGetProperty("items", out var _));
        Assert.False(root.TryGetProperty("bidAmount", out var _));
    }

    [Fact]
    public async Task ProposeTradeAsync_PostsTradeItems()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-trade","items":[]}""") };
        var client = Client(handler);
        var expires = DateTimeOffset.FromUnixTimeMilliseconds(1_790_000_000_000);
        var trade = new TradeTransactionBuilder(12, 10, 6).Send(100).Receive(200).Comment("deal").ExpiresAt(expires);
        await client.Transactions.ProposeTradeAsync(LeagueId, SeasonId, trade, TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var root = body.RootElement;
        Assert.Equal("TRADE_PROPOSAL", root.GetProperty("type").GetString());
        Assert.Equal("EXECUTE", root.GetProperty("executionType").GetString());
        Assert.Equal("deal", root.GetProperty("comment").GetString());
        Assert.Equal(1_790_000_000_000, root.GetProperty("expirationDate").GetInt64());
        Assert.Equal("{SWID-1}", root.GetProperty("memberId").GetString());
        var items = root.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("TRADE", items[0].GetProperty("type").GetString());
        Assert.Equal(100, items[0].GetProperty("playerId").GetInt32());
        Assert.Equal(12, items[0].GetProperty("fromTeamId").GetInt32());
        Assert.Equal(10, items[0].GetProperty("toTeamId").GetInt32());
        Assert.Equal(200, items[1].GetProperty("playerId").GetInt32());
        Assert.Equal(10, items[1].GetProperty("fromTeamId").GetInt32());
        Assert.Equal(12, items[1].GetProperty("toTeamId").GetInt32());
    }

    [Fact]
    public async Task RespondToTradeAsync_Accept_PostsRelatedId()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-accept","items":[]}""") };
        var client = Client(handler);
        await client.Transactions.RespondToTradeAsync(LeagueId, SeasonId, TradeTransactionBuilder.Accept(10, 6, "tx-9"), TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var root = body.RootElement;
        Assert.Equal("TRADE_ACCEPT", root.GetProperty("type").GetString());
        Assert.Equal("tx-9", root.GetProperty("relatedTransactionId").GetString());
        Assert.Equal(10, root.GetProperty("teamId").GetInt32());
        Assert.False(root.TryGetProperty("items", out var _));
    }

    [Fact]
    public async Task RespondToTradeAsync_Decline_PostsComment()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-decline","items":[]}""") };
        var client = Client(handler);
        await client.Transactions.RespondToTradeAsync(LeagueId, SeasonId, TradeTransactionBuilder.Decline(10, 6, "tx-9").Comment("no"), TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var root = body.RootElement;
        Assert.Equal("TRADE_DECLINE", root.GetProperty("type").GetString());
        Assert.Equal("no", root.GetProperty("comment").GetString());
        Assert.Equal("tx-9", root.GetProperty("relatedTransactionId").GetString());
    }

    [Fact]
    public async Task PostMessageAsync_PostsAuthorAndContent()
    {
        var handler = new RecordingHandler { Respond = _ => Json(PostedMessageJson) };
        var client = Client(handler);
        var posted = await client.Communication.PostMessageAsync(LeagueId, SeasonId, new ChatMessageBuilder(TopicId).Content("hello"), TestCt);
        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal(MessageUrl, call.Uri.AbsoluteUri);
        using var body = JsonDocument.Parse(call.Body!);
        Assert.Equal("{SWID-1}", body.RootElement.GetProperty("author").GetString());
        Assert.Equal("hello", body.RootElement.GetProperty("content").GetString());
        Assert.Equal(0, body.RootElement.GetProperty("messageTypeId").GetInt32());
        Assert.Equal("hello", posted.Content);
    }

    [Fact]
    public async Task PostMessageAsync_TopicType_ResolvesConversationId()
    {
        const string topicId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";
        var handler = new RecordingHandler {
            Respond = call => call.Method == HttpMethod.Get
                ? Json(
                    $$"""
                      {"topics":[
                        {"id":"{{topicId}}","type":"CHAT_ALL_MEMBERS","isDeleted":false},
                        {"id":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee","type":"CHAT_ALL_MEMBERS","isDeleted":true}
                      ]}
                      """)
                : Json(PostedMessageJson)
        };

        var client = Client(handler);
        await client.Communication.PostMessageAsync(LeagueId, SeasonId, new ChatMessageBuilder("CHAT_ALL_MEMBERS").Content("hello"), TestCt);
        Assert.Equal(2, handler.Calls.Count);
        Assert.Equal(HttpMethod.Get, handler.Calls[0].Method);
        Assert.Contains("/communication/?view=chat_conversations", handler.Calls[0].Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal(HttpMethod.Post, handler.Calls[1].Method);
        Assert.Contains($"/topics/{topicId}/messages/", handler.Calls[1].Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain("CHAT_ALL_MEMBERS", handler.Calls[1].Uri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostMessageAsync_AmbiguousTopicType_Throws()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json(
                """
                {"topics":[
                  {"id":"622adbe6-5001-33d1-82b0-d9d70c0cefcd","type":"CHAT_DIRECT_MESSAGE","isDeleted":false},
                  {"id":"11111111-2222-3333-4444-555555555555","type":"CHAT_DIRECT_MESSAGE","isDeleted":false}
                ]}
                """)
        };

        var client = Client(handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Communication.PostMessageAsync(
            LeagueId, SeasonId, new ChatMessageBuilder("CHAT_DIRECT_MESSAGE").Content("hello"), TestCt));

        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
    }

    [Fact]
    public async Task PostMessageAsync_MissingSwid_Throws()
    {
        var handler = new RecordingHandler();
        var client = new FantasyFootballClient(new() { EspnS2 = "s2-token" }, httpClient: new(handler));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.Communication.PostMessageAsync(LeagueId, SeasonId, new ChatMessageBuilder(TopicId).Content("hello"), TestCt));
        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task PostMessageAsync_ForAllMembers_PostsLeagueTopic()
    {
        const string topicId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";
        var handler = new RecordingHandler {
            Respond = call => call.Method == HttpMethod.Get ? Json($$"""{"topics":[{"id":"{{topicId}}","type":"CHAT_ALL_MEMBERS","isDeleted":false}]}""") : Json(PostedMessageJson)
        };

        var client = Client(handler);
        await client.Communication.PostMessageAsync(LeagueId, SeasonId, ChatMessageBuilder.For(ChatTopics.AllMembers).Content("hello"), TestCt);
        Assert.Equal(HttpMethod.Post, handler.Calls[1].Method);
        Assert.Contains($"/topics/{topicId}/messages/", handler.Calls[1].Uri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostMessageAsync_ToMember_PostsDirectMessage()
    {
        const string memberId = "{AAA-1111-2222-3333-444444444444}";
        const string otherId = "11111111-2222-3333-4444-555555555555";
        const string matchId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";
        const string deletedId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        var handler = new RecordingHandler {
            Respond = call => {
                if (call.Uri.AbsoluteUri.Contains(otherId, StringComparison.Ordinal))
                    return Json(ConversationJson(otherId, "{OTHER}"));

                if (call.Uri.AbsoluteUri.Contains(matchId, StringComparison.Ordinal) && call.Method == HttpMethod.Get)
                    return Json(ConversationJson(matchId, memberId));

                if (call.Method == HttpMethod.Get) {
                    return Json(
                        $$"""
                          {"topics":[
                            {"id":"{{deletedId}}","type":"CHAT_DIRECT_MESSAGE","isDeleted":true},
                            {"id":"{{otherId}}","type":"CHAT_DIRECT_MESSAGE","isDeleted":false},
                            {"id":"{{matchId}}","type":"CHAT_DIRECT_MESSAGE","isDeleted":false}
                          ]}
                          """);
                }

                return Json(PostedMessageJson);
            }
        };

        var client = Client(handler);
        await client.Communication.PostMessageAsync(LeagueId, SeasonId, ChatMessageBuilder.To(new MemberRes(memberId, "Ann", "Ann", "A")).Content("hello"), TestCt);
        Assert.Equal(4, handler.Calls.Count);
        Assert.Contains(otherId, handler.Calls[1].Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains(matchId, handler.Calls[2].Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.Equal(HttpMethod.Post, handler.Calls[3].Method);
        Assert.Contains($"/topics/{matchId}/messages/", handler.Calls[3].Uri.AbsoluteUri, StringComparison.Ordinal);
        Assert.DoesNotContain(handler.Calls, call => call.Uri.AbsoluteUri.Contains(deletedId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostMessageAsync_ToDisplayName_UsesLeagueIndex()
    {
        const string memberId = "{AAA-1111-2222-3333-444444444444}";
        const string matchId = "622adbe6-5001-33d1-82b0-d9d70c0cefcd";
        var handler = new RecordingHandler {
            Respond = call => call.Method == HttpMethod.Get
                ? Json(
                    $$"""
                      {"topics":[{"id":"{{matchId}}","type":"CHAT_DIRECT_MESSAGE","isDeleted":false,"viewableBy":["{{memberId}}"]}]}
                      """)
                : Json(PostedMessageJson)
        };

        var client = Client(handler);
        client.Remember(new(LeagueId, SeasonId, 6, Members: [new(memberId, "Ann", "Ann", "A")]));
        await client.Communication.PostMessageAsync(LeagueId, SeasonId, ChatMessageBuilder.To("Ann").Content("hello"), TestCt);
        Assert.Equal(2, handler.Calls.Count);
        Assert.Contains($"/topics/{matchId}/messages/", handler.Calls[1].Uri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostMessageAsync_ToUnknownMember_Throws()
    {
        var handler = new RecordingHandler();
        var client = Client(handler);
        client.Remember(new(LeagueId, SeasonId, 6, Members: [new("{AAA-1111-2222-3333-444444444444}", "Ann", "Ann", "A")]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Communication.PostMessageAsync(
            LeagueId, SeasonId, ChatMessageBuilder.To("Nobody").Content("hello"), TestCt));

        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task PostMessageAsync_ForAllMembersAndTo_Throws()
    {
        var handler = new RecordingHandler();
        var client = Client(handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Communication.PostMessageAsync(
            LeagueId, SeasonId, ChatMessageBuilder.For(ChatTopics.AllMembers).To(new MemberRes("{AAA-1111-2222-3333-444444444444}", "Ann", "Ann", "A")).Content("hello"), TestCt));

        Assert.Empty(handler.Calls);
    }

    [Fact]
    public async Task PostMessageAsync_ToMember_NoThread_Throws()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"topics":[{"id":"622adbe6-5001-33d1-82b0-d9d70c0cefcd","type":"CHAT_ALL_MEMBERS","isDeleted":false}]}""") };
        var client = Client(handler);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Communication.PostMessageAsync(
            LeagueId, SeasonId, ChatMessageBuilder.To(new MemberRes("{AAA-1111-2222-3333-444444444444}", "Ann", "Ann", "A")).Content("hello"), TestCt));

        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
    }

    [Fact]
    public async Task PostMessageAsync_ResponseArray_ReturnsPostedMessage()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json(
                """
                [{
                  "author": "{SWID-1}",
                  "content": "test again",
                  "date": 1792189573281,
                  "id": "3c092d43-4ae4-4234-ac5b-f1df6e5be9f1",
                  "isDeleted": false,
                  "isEdited": false,
                  "messageTypeId": 0,
                  "topicId": "622adbe6-5001-33d1-82b0-d9d70c0cefcd"
                }]
                """)
        };

        var client = Client(handler);
        var posted = await client.Communication.PostMessageAsync(LeagueId, SeasonId, new ChatMessageBuilder(TopicId).Content("test again"), TestCt);
        Assert.Equal("3c092d43-4ae4-4234-ac5b-f1df6e5be9f1", posted.Id);
        Assert.Equal("test again", posted.Content);
        Assert.False(posted.IsEdited);
    }

    [Fact]
    public async Task LineupTransactionBuilder_SlotConstants_PostsEspnSlotIds()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"id":"tx-lineup","items":[]}""") };
        var client = Client(handler);
        await client.Transactions.SetLineupAsync(LeagueId, SeasonId, new LineupTransactionBuilder(12, 6).Move(111, LineupSlot.Bench, LineupSlot.Quarterback), TestCt);
        using var body = JsonDocument.Parse(Assert.Single(handler.Calls).Body!);
        var item = Assert.Single(body.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(20, item.GetProperty("fromLineupSlotId").GetInt32());
        Assert.Equal(0, item.GetProperty("toLineupSlotId").GetInt32());
    }

    private static string ConversationJson(string topicId, string memberId)
        => $$"""
             {"id":"{{topicId}}","isDeleted":false,"isEdited":false,"totalMessageCount":0,"type":"CHAT_DIRECT_MESSAGE","unreadMessageCount":0,"viewableBy":["{{memberId}}"]}
             """;

    private static FantasyFootballClient Client(HttpMessageHandler handler) => new(new() { EspnS2 = "s2-token", Swid = "{SWID-1}" }, httpClient: new(handler));

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}