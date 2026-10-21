# Lyo.Leaderboard.Api

Embed with `AddLeaderboardApi` and `MapLeaderboardApiEndpoints`. Test hosts use `AddLeaderboardApiForWorkbench`, which skips algorithm registration when the catalog is already
present, and maps routes with authentication off.

## Features

- Routes under `/api/leaderboard` for boards, properties, members, invites, entries, adjustments, and standings.
- Workbench actor: unauthenticated requests act as a fixed owner. Header `X-Leaderboard-Actor: guest` is the second fixed user.
- `GET /s/{id}` expands a short URL and redirects.

## Examples

### Embed in a host

```csharp
services.AddLeaderboardApi(configuration);
app.MapLeaderboardApiEndpoints();
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms` (direct, lyo)
- `Lyo.Algorithms.Postgres` (direct, lyo)
- `Lyo.Api` (direct, lyo)
- `Lyo.Authentication` (direct, lyo)
- `Lyo.Authentication.AspNetCore` (direct, lyo)
- `Lyo.Authentication.Google` (direct, lyo)
- `Lyo.Authentication.Keycloak` (direct, lyo)
- `Lyo.Authentication.OpenIdConnect` (direct, lyo)
- `Lyo.Authentication.Postgres` (direct, lyo)
- `Lyo.Cache` (direct, lyo)
- `Lyo.Email` (direct, lyo)
- `Lyo.Json` (direct, lyo)
- `Lyo.KeyStore` (direct, lyo)
- `Lyo.Leaderboard` (direct, lyo)
- `Lyo.Leaderboard.Api.Models` (direct, lyo)
- `Lyo.Leaderboard.Postgres` (direct, lyo)
- `Lyo.QRCode` (direct, lyo)
- `Lyo.Query` (direct, lyo)
- `Lyo.ShortUrl` (direct, lyo)
- `Lyo.ShortUrl.Postgres` (direct, lyo)
- `Lyo.Algorithms.Models` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Authentication.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Email.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.EntityReference.Postgres` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Postgres` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Query.Sql` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `MailKit` `4.17.0` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SixLabors.ImageSharp` `3.1.12` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)
- `ZXing.Net` `0.16.11` (transitive, third-party)
- `ZXing.Net.Bindings.ImageSharp.V3` `0.16.18` (transitive, third-party)