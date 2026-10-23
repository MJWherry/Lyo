# Lyo.Espn.Fantasy.Football.Client

Typed client for ESPN fantasy football. League reads use `lm-api-reads`, transaction writes use `lm-api-writes`, Fantasy Chat uses `lm-api-communication`, and player news uses
`site.api.espn.com`. It subclasses `LyoHttpClient` and sends a browser User-Agent on every request so the chat host does not return 403.

## Examples

### Wire into DI ([`Extensions.cs`](Extensions.cs))

```csharp
services.AddFantasyFootballClientFromConfiguration(builder.Configuration);

// or:
services.AddFantasyFootballClient(o => {
    o.EspnS2 = "...";
    o.Swid = "{...}";
});
```

## Manager surface

[`FantasyFootballClient`](FantasyFootballClient.cs) exposes four manager properties; seasons before **2018** are rejected (checked by `ValidateSeason`).

| Property        | Manager                                           | Calls                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
|-----------------|---------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `League`        | [`LeagueManager`](LeagueManager.cs)               | `GetAsync(leagueId, seasonId)` (settings + teams + standings; also fills the in-memory league index), `GetTeamsAsync` (rosters for each scoring period), `GetTeamAsync` (one team), `GetDraftAsync` (draft picks), `GetMatchupsAsync` (scoreboard), `GetBoxScoreAsync` (one matchup with a typed roster).                                                                                                                                                                                                                                                                                                                                                                                                  |
| `Players`       | [`PlayerManager`](PlayerManager.cs)               | `GetInfoAsync(leagueId, seasonId, query)` (player cards through `kona_playercard`; writes the player cache), `GetPlayerAsync` (one player), `GetAsync(leagueId, seasonId, playerIds)` (cached cards, fetching only the misses), `GetFreeAgentsAsync` (`kona_player_info` for `FREEAGENT` and `WAIVERS`), `GetNewsAsync` (fantasy news feed).                                                                                                                                                                                                                                                                                                                                                               |
| `Communication` | [`CommunicationManager`](CommunicationManager.cs) | `GetLeagueChatAsync` (lists Fantasy Chat topics on `lm-api-communication`, then loads each conversation; pass a `LeagueRes` or a league id and season), `GetConversationAsync` (one topic; newest 25 messages by default; same league overload), `LoadPlayersAsync` (one player-card request for a conversation you already loaded), `GetRecentActivityAsync` (adds/drops/trades feed through `kona_league_communication`), `PostMessageAsync` (user text, `messageTypeId` 0). `ChatMessageBuilder.For(ChatTopics.AllMembers)` posts to the league chat. `ChatMessageBuilder.To(member)` posts to that member's direct message. The post response is a one-element array; the method returns that message. |
| `Transactions`  | [`TransactionsManager`](TransactionsManager.cs)   | `GetRecentAsync` (`mTransactions2` view, optional type filter), `GetRecentTradesAsync` (TRADE_* shorthand), `GetPendingAsync` (`mPendingTransactions`), `PostTransactionAsync`, `SetLineupAsync`, `AddDropAsync`, `CancelWaiverAsync`, `ProposeTradeAsync`, `RespondToTradeAsync`. Lineup moves accept [`LineupSlot`](LineupSlot.cs) (Bench is 20, Quarterback is 0).                                                                                                                                                                                                                                                                                                                                      |

[`FantasyFilterReqBuilder`](Builders/FantasyFilterReqBuilder.cs) builds `x-fantasy-filter` JSON. Write bodies come from [
`LineupTransactionBuilder`](Builders/LineupTransactionBuilder.cs), [`RosterTransactionBuilder`](Builders/RosterTransactionBuilder.cs), [
`TradeTransactionBuilder`](Builders/TradeTransactionBuilder.cs), and [`ChatMessageBuilder`](Builders/ChatMessageBuilder.cs). The manager methods post `Build()`; they do not
assemble the ESPN JSON themselves.

[`Remember`](FantasyFootballClient.cs) (also called from `League.GetAsync`) replaces [`LeagueIndex`](LeagueIndex.cs): members by SWID, teams by id and primary owner. `FindMember`
and `FindTeam` match one display name, team name, abbreviation, or "Location Nickname". Author `LM` is the league manager. [`PlayerCache`](PlayerCache.cs) stores cards by season
and player id. [`ToItemViewsAsync`](EspnLookups.cs) returns a structured copy of a transaction and does not change that ESPN record.

[`ChatMessageRes`](Models/Response/ChatMessageRes.cs) is the chat message a conversation returns. Raw ids and unix times keep a suffix: `AuthorId`, `DateUnix`, `DateEditedUnix`,
`ViewableByIds`, `TeamId`, and `PlayerId`. The unsuffixed name is the friendly value: `Author` and `Members` are league members, `Date` and `DateEdited` are timestamps, `Team` is
the league team, and `Player` is the card. `Kind` is `Text`, `AddDrop`, `Trade`, or `Unknown`. A trade (ESPN type 271, or keys such as `team1addP*` / `team2addP*` / `team1dropP*` /
`team2dropP*` / `tId`) has `Side1`, `Side2`, a transaction id, and every numbered player in numeric order. An add/drop (ESPN type 266, or `addP*` / `dropP*`) keeps every pickup and
drop on side 1. Types 0 and 270 are text. `GetLeagueChatAsync` and `GetConversationAsync` accept a league or a league id and season. They bind the ESPN JSON with the normal
serializer and copy it onto these models. `Author`, `Members`, and `Team` are filled from the league index when it already has them. Loading the league later does not change a chat
object you already have. They do not fetch cards. `Communication.LoadPlayersAsync` fills `Player` with one request. `Author` stays null for `LM`.

[`ChatTopics`](ChatTopics.cs) holds `AllMembers` and `DirectMessage`. [`ChatMessageBuilder.For`](Builders/ChatMessageBuilder.cs) takes one of those. [
`ChatMessageBuilder.To`](Builders/ChatMessageBuilder.cs) takes a member, a SWID, or a unique display name and posts to the existing direct message.

Request types sit under [`Models/Request`](Models/Request) (`PlayerInfoQuery`, `ChatConversationQuery`, `RecentActivityQuery`, `TransactionsQuery`, `FantasyFilterReq`,
`FantasyTransactionReq`, `ChatMessageReq`). Response types sit under [`Models/Response`](Models/Response) (`LeagueRes`, `TeamRes`, `MatchupRes`, `MatchupRosterRes`,
`PlayerInfoItemRes`, `PlayerNewsFeedRes`, `TransactionRes`, `ChatConversationRes`, `ChatMessageRes`). [`ChatConversationRes`](Models/Response/ChatConversationRes.cs) is that topic,
with the same id-and-model pairs.

## Settings ([`FantasyFootballClientOptions`](FantasyFootballClientOptions.cs))

Binds from section `FantasyFootballClient`. Picks up the `LyoHttpClientOptions` flags and adds:

| Property               | Description                                                                                                                  |
|------------------------|------------------------------------------------------------------------------------------------------------------------------|
| `EspnS2`               | Optional `espn_s2` cookie value. Needed for **private leagues**.                                                             |
| `Swid`                 | Optional `SWID` cookie value. Needed for **private leagues**.                                                                |
| `CommunicationBaseUrl` | Fantasy Chat host root. Defaults to `https://lm-api-communication.fantasy.espn.com/apis/v3/games/ffl/`.                      |
| `WritesBaseUrl`        | Lineup, waiver, and trade host root. Defaults to `https://lm-api-writes.fantasy.espn.com/apis/v3/games/ffl/`.                |
| `NewsBaseUrl`          | Fantasy player news URL. Defaults to `https://site.api.espn.com/apis/fantasy/v3/games/ffl/news/players`.                     |
| `UserAgent.Enabled`    | On by default. Sends the Chrome agent from `LyoHttpUserAgentOptions` on every request. The chat host returns 403 without it. |

`BaseUrl` defaults to `https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`. `CommunicationBaseUrl` defaults to the Fantasy Chat host. `WritesBaseUrl` defaults to the writes
host. `NewsBaseUrl` is the news feed URL. Public leagues skip cookies; both cookie values must be set before the client attaches them. Chat posts require `Swid`.

## Wire into DI ([`Extensions.cs`](Extensions.cs))

| Method                                                                     | Description                                                                                          |
|----------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------|
| `AddFantasyFootballClientFromConfiguration(configuration, sectionName?)`   | Reads `FantasyFootballClientOptions` from configuration (default section `"FantasyFootballClient"`). |
| `AddFantasyFootballClient(Action<FantasyFootballClientOptions> configure)` | Constructs options in place.                                                                         |
| `AddFantasyFootballClient(FantasyFootballClientOptions options)`           | Takes a ready-made options object.                                                                   |

Every overload registers `FantasyFootballClient` as a singleton and takes `ILoggerFactory` plus any registered `HttpClient` from DI.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DataTable.Models` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.RateLimit` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Bcl.TimeProvider` `8.0.1` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)