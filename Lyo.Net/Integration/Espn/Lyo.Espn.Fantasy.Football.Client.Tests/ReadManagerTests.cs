using System.Net;
using System.Text;
using System.Text.Json;

namespace Lyo.Espn.Fantasy.Football.Client.Tests;

public sealed class ReadManagerTests
{
    private const int LeagueId = 1001;
    private const int SeasonId = 2026;
    private const string ReadsRoot = "https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/seasons/2026/segments/0/leagues/1001";

    [Fact]
    public async Task GetFreeAgentsAsync_StatusFilter_RequestsKonaPlayerInfo()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"players":[{"id":42,"player":{"id":42,"fullName":"Waiver Wire"}}]}""") };
        var client = Client(handler);

        var players = await client.Players.GetFreeAgentsAsync(LeagueId, SeasonId, 6, ct: TestCt);

        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal($"{ReadsRoot}?view=kona_player_info&scoringPeriodId=6", call.Uri.AbsoluteUri);
        using (var filter = JsonDocument.Parse(call.Headers["x-fantasy-filter"])) {
            var playersFilter = filter.RootElement.GetProperty("players");
            var statuses = playersFilter.GetProperty("filterStatus").GetProperty("value").EnumerateArray().Select(item => item.GetString()!).ToArray();
            Assert.Equal(["FREEAGENT", "WAIVERS"], statuses);
            Assert.Equal(50, playersFilter.GetProperty("limit").GetInt32());
            Assert.Equal(0, playersFilter.GetProperty("offset").GetInt32());
            Assert.Equal(1, playersFilter.GetProperty("sortPercOwned").GetProperty("sortPriority").GetInt32());
            Assert.False(playersFilter.GetProperty("sortPercOwned").GetProperty("sortAsc").GetBoolean());
        }

        Assert.Equal("Waiver Wire", Assert.Single(players!).Player!.FullName);
        Assert.True(client.PlayerCache.TryGet(SeasonId, 42, out var cached));
        Assert.Equal("Waiver Wire", cached!.Player!.FullName);
    }

    [Fact]
    public async Task GetPendingAsync_RequestsPendingView()
    {
        var handler = new RecordingHandler { Respond = _ => Json("""{"transactions":[{"id":"pending-1","type":"WAIVER","isPending":true,"items":[]}]}""") };
        var client = Client(handler);

        var pending = await client.Transactions.GetPendingAsync(LeagueId, SeasonId, TestCt);

        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal($"{ReadsRoot}?view=mPendingTransactions", call.Uri.AbsoluteUri);
        Assert.Equal("WAIVER", Assert.Single(pending!).Type);
        Assert.True(pending![0].IsPending);
    }

    [Fact]
    public async Task GetBoxScoreAsync_TypesRosterEntries()
    {
        const string body = """
            {
              "schedule": [
                {
                  "id": 7,
                  "matchupPeriodId": 6,
                  "home": {
                    "teamId": 1,
                    "totalPoints": 100,
                    "rosterForCurrentScoringPeriod": {
                      "entries": [
                        {
                          "playerId": 99,
                          "lineupSlotId": 2,
                          "pendingTransactionIds": [],
                          "playerPoolEntry": { "id": 99, "appliedStatTotal": 18.5 }
                        }
                      ]
                    }
                  },
                  "away": { "teamId": 2, "totalPoints": 90 }
                }
              ]
            }
            """;
        var handler = new RecordingHandler { Respond = _ => Json(body) };
        var client = Client(handler);

        var matchup = await client.League.GetBoxScoreAsync(LeagueId, SeasonId, 6, 7, TestCt);

        var call = Assert.Single(handler.Calls);
        Assert.Equal($"{ReadsRoot}?view=mMatchup&view=mMatchupScore&view=mScoreboard&scoringPeriodId=6", call.Uri.AbsoluteUri);
        var entry = Assert.Single(matchup!.Home!.RosterForCurrentScoringPeriod!.Entries!);
        Assert.Equal(99, entry.PlayerId);
        Assert.Equal(18.5, entry.PlayerPoolEntry!.AppliedStatTotal);
        Assert.Null(matchup.Away!.RosterForCurrentScoringPeriod);
    }

    [Fact]
    public async Task GetNewsAsync_RequestsFantasyNewsFeed()
    {
        const string body = """
            {
              "resultsCount": 1,
              "feed": [
                { "playerId": 42, "headline": "Limited practice", "published": "2026-10-15T12:00:00Z", "description": "Ankle." }
              ]
            }
            """;
        var handler = new RecordingHandler { Respond = _ => Json(body) };
        var client = Client(handler);

        var news = await client.Players.GetNewsAsync(TestCt);

        var call = Assert.Single(handler.Calls);
        Assert.Equal(HttpMethod.Get, call.Method);
        Assert.Equal("https://site.api.espn.com/apis/fantasy/v3/games/ffl/news/players", call.Uri.AbsoluteUri);
        Assert.Equal(1, news!.ResultsCount);
        var item = Assert.Single(news.Feed!);
        Assert.Equal(42, item.PlayerId);
        Assert.Equal("Limited practice", item.Headline);
        Assert.Equal("2026-10-15T12:00:00Z", item.Published);
        Assert.Equal("Ankle.", item.Description);
    }

    private static FantasyFootballClient Client(HttpMessageHandler handler)
        => new(new FantasyFootballClientOptions { EspnS2 = "s2-token", Swid = "{SWID-1}" }, httpClient: new HttpClient(handler));

    private static HttpResponseMessage Json(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
}
