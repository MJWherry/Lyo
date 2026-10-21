# Lyo.FantasyFootball

League, member, team, roster, conversation, message, and trade records, plus `IFantasyFootballStore`. No Entity Framework and no ESPN types. The HTTP API maps ESPN into these
records.

## Features

- `IFantasyFootballStore` upserts a league by ESPN league id and season, then stores chats and trades under that league.
- A roster row is one player for the league scoring period, so the trade form does not call ESPN.
- Conversation members are SWIDs already saved on the league.

## Examples

### Save a league

```csharp
await store.SaveLeagueAsync(league, cancellationToken);
```