using System.Net;
using System.Text;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;

namespace Lyo.Espn.Fantasy.Football.Client.Tests;

public sealed class ChatActivityTests
{
    private const int LeagueId = 1001;
    private const int SeasonId = 2026;

    [Fact]
    public async Task ChatActivity_TradeMessage_ReadsBothSides()
    {
        var message = await Bind(
            271, "4dede841-88e4-40ec-bff1-e50d960295db",
            new Dictionary<string, string> {
                ["team2addP1"] = "4887558",
                ["team1"] = "4",
                ["team2"] = "9",
                ["team1addP1"] = "4569173",
                ["tId"] = "3ee65234-faff-4ea2-a708-5c377a55a26f"
            });

        Assert.Equal(ChatMessageKind.Trade, message.Kind);
        Assert.Equal(4, message.Side1!.TeamId);
        Assert.Equal(4569173, Assert.Single(message.Side1.Added).PlayerId);
        Assert.Equal(9, message.Side2!.TeamId);
        Assert.Equal(4887558, Assert.Single(message.Side2.Added).PlayerId);
        Assert.Equal("3ee65234-faff-4ea2-a708-5c377a55a26f", message.TransactionId);
        Assert.Null(message.Content);
        Assert.Equal(1, message.DateUnix);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1), message.Date);
        Assert.Equal("LM", message.AuthorId);
        Assert.Null(message.Side1!.Added[0].Player);
    }

    [Fact]
    public async Task ChatActivity_AddDropMessage_ReadsAddAndDrop()
    {
        var message = await Bind(266, "77426f21-f1db-4d97-8e2d-b1a242686a17", new Dictionary<string, string> { ["team1"] = "1", ["addP1"] = "-16029", ["dropP1"] = "-16034" });

        Assert.Equal(ChatMessageKind.AddDrop, message.Kind);
        Assert.Equal(1, message.Side1!.TeamId);
        Assert.Equal(-16029, Assert.Single(message.Side1.Added).PlayerId);
        Assert.Equal(-16034, Assert.Single(message.Side1.Dropped).PlayerId);
        Assert.Null(message.Side2);
    }

    [Fact]
    public async Task ChatActivity_MultiPlayerAddDrop_KeepsEveryPlayer()
    {
        var message = await Bind(266, "add-many", new Dictionary<string, string> { ["team1"] = "1", ["addP3"] = "33", ["dropP2"] = "22", ["addP1"] = "11", ["dropP1"] = "21" });

        Assert.Equal(ChatMessageKind.AddDrop, message.Kind);
        Assert.Equal([11, 33], message.Side1!.Added.Select(player => player.PlayerId).ToArray());
        Assert.Equal([21, 22], message.Side1.Dropped.Select(player => player.PlayerId).ToArray());
        Assert.Null(message.Side2);
    }

    [Fact]
    public async Task ChatActivity_TextMessage_UsesContent()
    {
        var handler = new RecordingHandler { Respond = _ => Json(Conversation(270, "recap", content: "What a matchup! Check out your league's recap in the Fantasy app.")) };
        var client = Client(handler);

        var conversation = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "topic", ct: TestCt);

        var message = Assert.Single(conversation!.Messages!);
        Assert.Equal(ChatMessageKind.Text, message.Kind);
        Assert.Equal("What a matchup! Check out your league's recap in the Fantasy app.", message.Content);
        Assert.Null(message.Side1);
        Assert.Null(message.Side2);
        Assert.Equal("LM", message.AuthorId);
        Assert.True(message.FromLeagueManager);
        Assert.DoesNotContain(handler.Calls, call => call.Uri.AbsoluteUri.Contains("kona_playercard", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ChatActivity_MultiPlayerTrade_KeepsEveryPlayer()
    {
        var message = await Bind(
            271, "trade-many",
            new Dictionary<string, string> {
                ["team1"] = "4",
                ["team2"] = "9",
                ["team1addP3"] = "3",
                ["team1addP1"] = "1",
                ["team1dropP2"] = "12",
                ["team2addP2"] = "22",
                ["team2addP1"] = "21"
            });

        Assert.Equal(ChatMessageKind.Trade, message.Kind);
        Assert.Equal([1, 3], message.Side1!.Added.Select(player => player.PlayerId).ToArray());
        Assert.Equal([12], message.Side1.Dropped.Select(player => player.PlayerId).ToArray());
        Assert.Equal([21, 22], message.Side2!.Added.Select(player => player.PlayerId).ToArray());
        Assert.Empty(message.Side2.Dropped);
    }

    private static async Task<ChatMessageRes> Bind(int typeId, string id, Dictionary<string, string>? attributes = null, string? content = null)
    {
        var handler = new RecordingHandler { Respond = _ => Json(Conversation(typeId, id, attributes, content)) };
        var client = Client(handler);
        var conversation = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "topic", ct: TestCt);
        return Assert.Single(conversation!.Messages!);
    }

    private static string Conversation(int typeId, string id, Dictionary<string, string>? attributes = null, string? content = null)
    {
        var contentJson = content == null ? "" : $",\"content\":{System.Text.Json.JsonSerializer.Serialize(content)}";
        var attributeJson = attributes == null ? "" : $",\"attributes\":{{{string.Join(",", attributes.Select(pair => $"\"{pair.Key}\":\"{pair.Value}\""))}}}";
        var message = $$"""{"id":"{{id}}","author":"LM","date":1,"isDeleted":false,"messageTypeId":{{typeId}},"topicId":"topic"{{contentJson}}{{attributeJson}}}""";
        return $$"""{"id":"topic","author":"LM","date":1,"isDeleted":false,"isEdited":false,"messages":[{{message}}],"totalMessageCount":1,"type":"CHAT_ALL_MEMBERS","unreadMessageCount":0}""";
    }

    private static FantasyFootballClient Client(HttpMessageHandler handler)
        => new(new FantasyFootballClientOptions { EspnS2 = "s2-token", Swid = "{SWID-1}" }, httpClient: new HttpClient(handler));

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
