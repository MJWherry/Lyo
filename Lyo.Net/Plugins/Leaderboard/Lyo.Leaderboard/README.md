# Lyo.Leaderboard

Domain for user-created games. Each board has one score formula. Choice options store a key and a number the formula can read. This package aggregates approved entry totals (sum,
best, best-N, average, count, latest).

## Features

- `LeaderboardAggregator` collapses approved entry scores into a member total.
- `ILeaderboardStore` is the persistence and policy boundary. Board roles stay on the member row.
- Global scopes `leaderboard.read` / `leaderboard.write` / `leaderboard.admin` via `AddLeaderboardScopes`.
- Invite tokens are plaintext once; only the SHA-256 hash is stored.

## Examples

### Aggregate approved scores

```csharp
var total = LeaderboardAggregator.Aggregate(AggregationKind.Sum, null, SortDirection.Desc, scores);
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Authentication` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.Result` (direct, lyo)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)