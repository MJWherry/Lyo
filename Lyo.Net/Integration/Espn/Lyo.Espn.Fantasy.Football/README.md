# Lyo.Espn.Fantasy.Football

Typed read-only client for the ESPN fantasy football v3 API (`lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`). Subclasses `Lyo.Api.Client.ApiClient` so JSON serialization,
Accept-Encoding, and request compression match the rest of the Lyo HTTP-client family.

## Surface

[`FantasyFootballClient`](FantasyFootballClient.cs) wires four manager properties; only seasons from **2018** onward are supported (validated by `ValidateSeason`).

| Property         | Manager                                                                | Operations                                                                                                                                                                                          |
|------------------|------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `League`         | [`LeagueManager`](LeagueManager.cs)                                    | `GetAsync(leagueId, seasonId)` (settings + teams + standings), `GetTeamsAsync` (rosters per scoring period), `GetTeamAsync` (single team), `GetDraftAsync` (draft picks), `GetMatchupsAsync` (scoreboard). |
| `Players`        | [`PlayerManager`](PlayerManager.cs)                                    | `GetInfoAsync(leagueId, seasonId, query)` (player cards via `kona_playercard`), `GetPlayerAsync` (single player).                                                                                  |
| `Communication`  | [`CommunicationManager`](CommunicationManager.cs)                      | `GetLeagueChatAsync` (message board, optional topic-type filter), `GetRecentActivityAsync` (adds/drops/trades feed via `kona_league_communication`).                                              |
| `Transactions`   | [`TransactionsManager`](TransactionsManager.cs)                        | `GetRecentAsync` (`mTransactions2` view, optional type filter), `GetRecentTradesAsync` (TRADE_* shorthand).                                                                                       |

Internal helpers `BuildLeaguePath`, `GetLeagueViewAsync`, and `ApplyAuthentication` (cookie-based) handle URL composition, the `x-fantasy-filter` header, and authentication. The
`x-fantasy-filter` JSON is built by [`FantasyFilterReqBuilder`](Builders) per call (player ids, transaction types, message-board topics, recent activity windows).

Models live under [`Models/Request`](Models) (`PlayerInfoQuery`, `LeagueChatQuery`, `RecentActivityQuery`, `TransactionsQuery`, `FantasyFilterReq`) and `Models/Response`
(`LeagueRes`, `TeamRes`, `MatchupRes`, `DraftResponseRes`, `PlayerInfoItemRes`, `CommunicationTopicRes`, `TransactionRes`, `LeagueChatRes`, etc.).

## Options ([`FantasyFootballClientOptions`](FantasyFootballClientOptions.cs))

Configuration section: `FantasyFootballClient` (shadows the base `ApiClient` section). Inherits all
[`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flags and adds:

| Property   | Description                                                                                              |
|------------|----------------------------------------------------------------------------------------------------------|
| `EspnS2`   | Optional value of the `espn_s2` cookie. Required for **private leagues**.                                |
| `Swid`     | Optional value of the `SWID` cookie. Required for **private leagues**.                                   |

Default `BaseUrl` is `https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`. Public leagues do not need cookies; the client only sends them when both values are set.

## DI registration ([`Extensions.cs`](Extensions.cs))

| Method                                                                       | Description                                                                                                       |
|------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------|
| `AddFantasyFootballClientFromConfiguration(configuration, sectionName?)`     | Binds `FantasyFootballClientOptions` from configuration (default section `"FantasyFootballClient"`).               |
| `AddFantasyFootballClient(Action<FantasyFootballClientOptions> configure)`   | Builds options inline.                                                                                            |
| `AddFantasyFootballClient(FantasyFootballClientOptions options)`             | Registers a pre-built options instance.                                                                           |

All overloads register `FantasyFootballClient` as a singleton, pulling `ILoggerFactory` and any registered `HttpClient` from DI.

```csharp
services.AddFantasyFootballClientFromConfiguration(builder.Configuration);

// or:
services.AddFantasyFootballClient(o => {
    o.EspnS2 = "...";
    o.Swid = "{...}";
});
```

## Related projects

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md) — base HTTP client.
