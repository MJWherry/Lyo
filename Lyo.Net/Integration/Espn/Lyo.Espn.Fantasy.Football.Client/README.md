# Lyo.Espn.Fantasy.Football.Client

Read-only typed client for ESPN's fantasy football v3 API (`lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`). It subclasses `Lyo.Api.Client.ApiClient` so JSON serialization, Accept-Encoding, and request compression stay consistent with the rest of the Lyo HTTP-client family.

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

| Property | Manager | Calls |
| --------------- | ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `League` | [`LeagueManager`](LeagueManager.cs) | `GetAsync(leagueId, seasonId)` (settings + teams + standings), `GetTeamsAsync` (rosters for each scoring period), `GetTeamAsync` (one team), `GetDraftAsync` (draft picks), `GetMatchupsAsync` (scoreboard). |
| `Players` | [`PlayerManager`](PlayerManager.cs) | `GetInfoAsync(leagueId, seasonId, query)` (player cards through `kona_playercard`), `GetPlayerAsync` (one player). |
| `Communication` | [`CommunicationManager`](CommunicationManager.cs) | `GetLeagueChatAsync` (message board, optional topic-type filter), `GetRecentActivityAsync` (adds/drops/trades feed through `kona_league_communication`). |
| `Transactions` | [`TransactionsManager`](TransactionsManager.cs) | `GetRecentAsync` (`mTransactions2` view, optional type filter), `GetRecentTradesAsync` (TRADE_* shorthand). |

URL composition, the `x-fantasy-filter` header, and cookie auth go through internal helpers `BuildLeaguePath`, `GetLeagueViewAsync`, and `ApplyAuthentication`. [`FantasyFilterReqBuilder`](Builders) builds the
`x-fantasy-filter` JSON per call (player ids, transaction types, message-board topics, recent activity windows).

Request types sit under [`Models/Request`](Models) (`PlayerInfoQuery`, `LeagueChatQuery`, `RecentActivityQuery`, `TransactionsQuery`, `FantasyFilterReq`); response types sit under `Models/Response`
(`LeagueRes`, `TeamRes`, `MatchupRes`, `DraftResponseRes`, `PlayerInfoItemRes`, `CommunicationTopicRes`, `TransactionRes`, `LeagueChatRes`, etc.).

## Settings ([`FantasyFootballClientOptions`](FantasyFootballClientOptions.cs))

Binds from section `FantasyFootballClient` (overrides the base `ApiClient` section). Picks up every
[`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flag and adds:

| Property | Description |
| -------- | ---------------------------------------------------------------- |
| `EspnS2` | Optional `espn_s2` cookie value. Needed for **private leagues**. |
| `Swid` | Optional `SWID` cookie value. Needed for **private leagues**. |

`BaseUrl` defaults to `https://lm-api-reads.fantasy.espn.com/apis/v3/games/ffl/`. Public leagues skip cookies; both values must be set before the client attaches them.

## Wire into DI ([`Extensions.cs`](Extensions.cs))

| Method | Description |
| -------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| `AddFantasyFootballClientFromConfiguration(configuration, sectionName?)` | Reads `FantasyFootballClientOptions` from configuration (default section `"FantasyFootballClient"`). |
| `AddFantasyFootballClient(Action<FantasyFootballClientOptions> configure)` | Constructs options in place. |
| `AddFantasyFootballClient(FantasyFootballClientOptions options)` | Takes a ready-made options object. |

Every overload registers `FantasyFootballClient` as a singleton and takes `ILoggerFactory` plus any registered `HttpClient` from DI.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)