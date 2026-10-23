namespace Lyo.FantasyFootball.Web.Components.Tests;

public sealed class FantasyFootballUiTests
{
    [Fact]
    public void TopicTitle_DirectMessage_UsesTheOtherMember()
    {
        var conversation = new FantasyConversationRecord {
            Type = "CHAT_DIRECT_MESSAGE", Members = [new() { EspnMemberId = "ann", DisplayName = "Ann" }, new() { EspnMemberId = "bob", DisplayName = "Bob" }]
        };

        Assert.Equal("Bob", FantasyFootballUi.TopicTitle(conversation, "ann"));
    }

    [Fact]
    public void TopicTitle_AllMembers_IsLeagueChat()
    {
        var conversation = new FantasyConversationRecord { Type = "CHAT_ALL_MEMBERS", Members = [new() { EspnMemberId = "ann", DisplayName = "Ann" }] };
        Assert.Equal("League chat", FantasyFootballUi.TopicTitle(conversation, "ann"));
    }

    [Fact]
    public void TradeSummary_TwoSides_ListsSentAndReceivedPlayers()
    {
        var trade = new FantasyTradeRecord {
            Items = [
                new() {
                    FromEspnTeamId = 1,
                    ToEspnTeamId = 2,
                    PlayerId = 100,
                    PlayerName = "Alpha"
                },
                new() {
                    FromEspnTeamId = 2,
                    ToEspnTeamId = 1,
                    PlayerId = 200,
                    PlayerName = "Beta"
                }
            ]
        };

        Assert.Equal("Sent Alpha. Received Beta.", FantasyFootballUi.TradeSummary(trade));
    }
}