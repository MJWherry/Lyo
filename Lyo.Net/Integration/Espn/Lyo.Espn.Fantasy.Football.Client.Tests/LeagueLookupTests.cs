using System.Net;
using System.Text;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;

namespace Lyo.Espn.Fantasy.Football.Client.Tests;

public sealed class LeagueLookupTests
{
    private const int LeagueId = 1001;
    private const int SeasonId = 2026;
    private const string AnnId = "{AAA-1111-2222-3333-444444444444}";
    private const string BobId = "{BBB-1111-2222-3333-444444444444}";

    [Fact]
    public async Task GetAsync_Remember_IndexesMembers()
    {
        var handler = new RecordingHandler {
            Respond = _ => Json($$"""{"id":{{LeagueId}},"seasonId":{{SeasonId}},"scoringPeriodId":6,"members":[{"id":"{{AnnId}}","displayName":"Ann"}]}""")
        };

        var client = Client(handler);
        await client.League.GetAsync(LeagueId, SeasonId, TestCt);
        Assert.Equal("Ann", client.LeagueDirectory!.Member(AnnId.ToLowerInvariant())!.DisplayName);
    }

    [Fact]
    public void FindMember_AmbiguousName_Throws()
    {
        var index = new LeagueIndex(new(LeagueId, SeasonId, 6, Members: [new("{A}", "Ann", "Ann", "One"), new("{B}", "Ann", "Bob", "Two")]));
        Assert.Throws<InvalidOperationException>(() => index.FindMember("ann"));
    }

    [Fact]
    public void FindTeam_ByAbbrevOrNickname_ReturnsTeam()
    {
        var index = new LeagueIndex(new(LeagueId, SeasonId, 6, Teams: [Team(12, "Ann Team", AnnId) with { Abbrev = "ANN", Location = "Ann", Nickname = "Squad" }]));
        Assert.Equal(12, index.FindTeam("ANN").Id);
        Assert.Equal(12, index.FindTeam("Ann Squad").Id);
    }

    [Fact]
    public async Task LeagueIndex_DirectMessage_ResolvesBothMembers()
    {
        var handler = new RecordingHandler { Respond = _ => Json(DirectMessageJson()) };
        var client = Client(handler);
        client.Remember(SampleLeague());
        var conversation = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "dm-1", ct: TestCt);
        Assert.Equal([AnnId.ToLowerInvariant(), BobId], conversation!.ViewableByIds);
        Assert.Equal(["Ann", "Bob"], conversation.Members.Select(member => member.DisplayName!).ToArray());
        var message = Assert.Single(conversation.Messages!);
        Assert.Equal(AnnId.ToLowerInvariant(), message.AuthorId);
        Assert.Equal("Ann", message.Author!.DisplayName);
        Assert.False(message.FromLeagueManager);
        Assert.DoesNotContain(handler.Calls, call => call.Uri.AbsoluteUri.Contains("kona_playercard", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LeagueIndex_RememberAfterChat_LeavesAuthorEmpty()
    {
        var handler = new RecordingHandler { Respond = _ => Json(DirectMessageJson()) };
        var client = Client(handler);
        var first = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "dm-1", ct: TestCt);
        Assert.Null(first!.Author);
        Assert.Empty(first.Members);
        client.Remember(SampleLeague());
        Assert.Null(first.Author);
        Assert.Empty(first.Members);
        var second = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "dm-1", ct: TestCt);
        Assert.Equal("Ann", second!.Author!.DisplayName);
        Assert.Equal(["Ann", "Bob"], second.Members.Select(member => member.DisplayName!).ToArray());
    }

    [Fact]
    public async Task PlayerCache_AddDropMessage_ResolvesTeamAndPlayers()
    {
        var handler = new RecordingHandler {
            Respond = call => call.Uri.AbsoluteUri.Contains("kona_playercard", StringComparison.Ordinal)
                ? Json(PlayersJson(4832955, "Added Guy", 3149687, "Dropped Guy"))
                : Json(AddDropJson())
        };

        var client = Client(handler);
        client.Remember(SampleLeague());
        var conversation = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "topic-1", ct: TestCt);
        var message = Assert.Single(conversation!.Messages!);
        Assert.Equal("LM", message.AuthorId);
        Assert.Null(message.Author);
        Assert.True(message.FromLeagueManager);
        Assert.Equal(12, message.Side1!.TeamId);
        Assert.Equal("Ann Team", message.Side1.Team!.Name);
        Assert.Null(message.Side1.Added[0].Player);
        await client.Communication.LoadPlayersAsync(conversation, LeagueId, SeasonId, TestCt);
        await client.Communication.LoadPlayersAsync(conversation, LeagueId, SeasonId, TestCt);
        Assert.Single(handler.Calls, call => call.Uri.AbsoluteUri.Contains("kona_playercard", StringComparison.Ordinal));
        Assert.Equal(ChatMessageKind.AddDrop, message.Kind);
        Assert.Equal(4832955, message.Side1.Added[0].PlayerId);
        Assert.Equal("Added Guy", message.Side1.Added[0].Player!.Player!.FullName);
        Assert.Equal("Dropped Guy", message.Side1.Dropped[0].Player!.Player!.FullName);
        Assert.Null(message.Side2);
    }

    [Fact]
    public async Task LeagueIndex_TradeMessage_ResolvesBothSides()
    {
        var handler = new RecordingHandler {
            Respond = call => call.Uri.AbsoluteUri.Contains("kona_playercard", StringComparison.Ordinal) ? Json(PlayersJson(100, "Sent", 200, "Received")) : Json(TradeJson())
        };

        var client = Client(handler);
        client.Remember(SampleLeague());
        var conversation = await client.Communication.GetConversationAsync(LeagueId, SeasonId, "trade-topic", ct: TestCt);
        await client.Communication.LoadPlayersAsync(conversation!, SampleLeague(), TestCt);
        var message = Assert.Single(conversation!.Messages!);
        Assert.Equal(ChatMessageKind.Trade, message.Kind);
        Assert.Equal(12, message.Side1!.TeamId);
        Assert.Equal(10, message.Side2!.TeamId);
        Assert.Equal("Ann Team", message.Side1.Team!.Name);
        Assert.Equal("Bob Team", message.Side2.Team!.Name);
        Assert.Equal(100, message.Side1.Added[0].PlayerId);
        Assert.Equal("Sent", message.Side1.Added[0].Player!.Player!.FullName);
        Assert.Equal("Received", message.Side2.Added[0].Player!.Player!.FullName);
        var transaction = new TransactionRes(
            "tx-trade", "TRADE_ACCEPT", null, null, 6, null, null, null, null, null, null, null,
            [new("TRADE", 12, 10, 100, null, null, null), new("DROP", 10, null, 200, null, null, null)]);

        var items = await transaction.ToItemViewsAsync(client, LeagueId, SeasonId, TestCt);
        Assert.Single(handler.Calls, call => call.Uri.AbsoluteUri.Contains("kona_playercard", StringComparison.Ordinal));
        Assert.Equal("Ann Team", items[0].FromTeam!.Name);
        Assert.Equal("Bob Team", items[0].ToTeam!.Name);
        Assert.Equal("Sent", items[0].Player!.Player!.FullName);
        Assert.Equal("Bob Team", items[1].FromTeam!.Name);
        Assert.Null(items[1].ToTeam);
        Assert.Equal("Received", items[1].Player!.Player!.FullName);
    }

    private static string DirectMessageJson()
        => $$"""
             {
               "id":"dm-1","author":"{{AnnId}}","date":1,"isDeleted":false,"isEdited":false,
               "messages":[{"id":"m1","author":"{{AnnId.ToLowerInvariant()}}","content":"hey","date":2,"isDeleted":false,"messageTypeId":0,"topicId":"dm-1"}],
               "totalMessageCount":1,"type":"CHAT_DIRECT_MESSAGE","unreadMessageCount":0,
               "viewableBy":["{{AnnId.ToLowerInvariant()}}","{{BobId}}"]
             }
             """;

    private static string AddDropJson()
        => """
           {
             "id":"topic-1","author":"LM","date":1,"isDeleted":false,"isEdited":false,
             "messages":[{"id":"m-add","author":"LM","date":2,"isDeleted":false,"messageTypeId":266,"topicId":"topic-1","attributes":{"team1":"12","addP1":"4832955","dropP1":"3149687"}}],
             "totalMessageCount":1,"type":"CHAT_ALL_MEMBERS","unreadMessageCount":0
           }
           """;

    private static string TradeJson()
        => """
           {
             "id":"trade-topic","author":"LM","date":1,"isDeleted":false,"isEdited":false,
             "messages":[{"id":"m-trade","author":"LM","date":2,"isDeleted":false,"messageTypeId":271,"topicId":"trade-topic","attributes":{"team1":"12","team2":"10","team1addP1":"100","team2addP1":"200","tId":"tx-1"}}],
             "totalMessageCount":1,"type":"CHAT_ALL_MEMBERS","unreadMessageCount":0
           }
           """;

    private static LeagueRes SampleLeague()
        => new(LeagueId, SeasonId, 6, Members: [new(AnnId, "Ann", "Ann", "A"), new(BobId, "Bob", "Bob", "B")], Teams: [Team(12, "Ann Team", AnnId), Team(10, "Bob Team", BobId)]);

    private static TeamRes Team(int id, string name, string owner) => new(id, null, null, null, name, null, owner, null, null, null, null, null, null, null, null, null, null);

    private static string PlayersJson(int firstId, string firstName, int secondId, string secondName)
        => $$$"""
              {"players":[
                {"id":{{{firstId}}},"player":{"id":{{{firstId}}},"fullName":"{{{firstName}}}"}} ,
                {"id":{{{secondId}}},"player":{"id":{{{secondId}}},"fullName":"{{{secondName}}}"}}
              ]}
              """;

    private static FantasyFootballClient Client(HttpMessageHandler handler) => new(new() { EspnS2 = "s2-token", Swid = "{SWID-1}" }, httpClient: new(handler));

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}