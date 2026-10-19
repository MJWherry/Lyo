using Microsoft.Extensions.DependencyInjection;

namespace Lyo.FantasyFootball.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class PostgresFantasyFootballStoreTests(FantasyFootballPostgresFixture fixture)
{
    [Fact]
    public async Task SaveLeague_SameEspnIds_UpsertsMembersAndTeams()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>();
        var first = League(4101, Member("ann", "Ann"), Team(1, "Alpha", Player(100, "Alpha")));
        await store.SaveLeagueAsync(first, TestCt);

        var second = League(4101, Member("ann", "Ann Marie"), Member("bob", "Bob"), Team(1, "Alpha Prime", Player(100, "Alpha")));
        var saved = await store.SaveLeagueAsync(second, TestCt);

        Assert.Equal("Ann Marie", Assert.Single(saved.Members, member => member.EspnMemberId == "ann").DisplayName);
        Assert.Equal("Bob", Assert.Single(saved.Members, member => member.EspnMemberId == "bob").DisplayName);
        Assert.Equal(2, saved.Members.Count);
        Assert.Equal("Alpha Prime", Assert.Single(saved.Teams).Name);
        Assert.Equal("Alpha", Assert.Single(Assert.Single(saved.Teams).Roster).PlayerName);
    }

    [Fact]
    public async Task SaveConversations_DirectMessage_ListsTheOtherMember()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>();
        await store.SaveLeagueAsync(League(4102, Member("ann", "Ann"), Member("bob", "Bob"), Team(1, "Alpha")), TestCt);
        await store.SaveConversationsAsync(4102, 2026, [
            new FantasyConversationRecord {
                EspnTopicId = "dm-1",
                Type = "CHAT_DIRECT_MESSAGE",
                UnreadCount = 1,
                Members = [Member("ann", "Ann"), Member("bob", "Bob")]
            }
        ], TestCt);

        var conversation = Assert.Single(await store.ListConversationsAsync(4102, 2026, TestCt));
        Assert.Equal("CHAT_DIRECT_MESSAGE", conversation.Type);
        Assert.Equal(1, conversation.UnreadCount);
        Assert.Equal(new[] { "Ann", "Bob" }, conversation.Members.Select(member => member.DisplayName).OrderBy(name => name).ToArray());
    }

    [Fact]
    public async Task SaveMessages_Conversation_ReturnsThemByTime()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>();
        await store.SaveLeagueAsync(League(4103, Member("ann", "Ann"), Team(1, "Alpha")), TestCt);
        await store.SaveConversationsAsync(4103, 2026, [new FantasyConversationRecord { EspnTopicId = "topic-1", Type = "CHAT_ALL_MEMBERS" }], TestCt);
        await store.SaveMessagesAsync(4103, 2026, "topic-1", [
            Message("later", "second", sentAt: 20),
            Message("earlier", "first", sentAt: 10)
        ], TestCt);

        var messages = await store.ListMessagesAsync(4103, 2026, "topic-1", TestCt);
        Assert.Equal(new[] { "first", "second" }, messages.Select(message => message.Content).ToArray());
    }

    [Fact]
    public async Task SaveTrades_SendAndReceive_StoresBothItems()
    {
        var store = fixture.ServiceProvider.GetRequiredService<IFantasyFootballStore>();
        await store.SaveLeagueAsync(League(4104, Member("ann", "Ann"), Team(1, "Alpha"), Team(2, "Beta")), TestCt);
        await store.SaveTradesAsync(4104, 2026, [
            new FantasyTradeRecord {
                EspnTransactionId = "tx-1",
                Type = "TRADE_PROPOSAL",
                IsPending = true,
                Items = [
                    new FantasyTradeItemRecord { FromEspnTeamId = 1, ToEspnTeamId = 2, PlayerId = 100, PlayerName = "Sent" },
                    new FantasyTradeItemRecord { FromEspnTeamId = 2, ToEspnTeamId = 1, PlayerId = 200, PlayerName = "Received" }
                ]
            }
        ], TestCt);

        var trade = Assert.Single(await store.ListTradesAsync(4104, 2026, pending: true, ct: TestCt));
        Assert.Equal([(1, 2, 100), (2, 1, 200)], trade.Items.Select(item => (item.FromEspnTeamId, item.ToEspnTeamId, item.PlayerId)).ToArray());
    }

    private static FantasyLeagueRecord League(int espnLeagueId, params object[] parts)
    {
        var members = new List<FantasyMemberRecord>();
        var teams = new List<FantasyTeamRecord>();
        foreach (var part in parts) {
            if (part is FantasyMemberRecord member)
                members.Add(member);
            else if (part is FantasyTeamRecord team)
                teams.Add(team);
        }

        return new FantasyLeagueRecord { EspnLeagueId = espnLeagueId, SeasonId = 2026, ScoringPeriodId = 6, Name = "League", Members = members, Teams = teams };
    }

    private static FantasyMemberRecord Member(string id, string name) => new() { EspnMemberId = id, DisplayName = name };

    private static FantasyTeamRecord Team(int id, string name, params FantasyRosterEntryRecord[] roster)
        => new() { EspnTeamId = id, Name = name, Roster = roster };

    private static FantasyRosterEntryRecord Player(int id, string name) => new() { ScoringPeriodId = 6, PlayerId = id, PlayerName = name };

    private static FantasyMessageRecord Message(string id, string content, int sentAt)
        => new() { EspnMessageId = id, Content = content, Kind = "Text", SentAt = DateTimeOffset.FromUnixTimeMilliseconds(sentAt), AuthorEspnId = "ann" };
}
