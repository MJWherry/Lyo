# Lyo.Discord.Client

Typed HTTP client for the Discord REST endpoints `Lyo.Api` exposes (the `Discord/*` group registered by [`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md)). Wraps `Lyo.Api.Client.ApiClient` so Accept-Encoding, request compression, and problem-details parsing match the other Lyo clients.

## Examples

### Wire into DI ([`Extensions.cs`](Extensions.cs))

```csharp
services.AddDiscordClientFromConfiguration(builder.Configuration);

// or:
services.AddDiscordClient(o => {
    o.BaseUrl = "https://api.example.com/";
    o.RequestCompression = ApiRequestCompressionType.Gzip;
});
```

## Manager surface

[`LyoDiscordClient`](LyoDiscordClient.cs) is the entry point. It subclasses `ApiClient` and exposes nine **manager** properties under `Managers/`:

| Property | Manager | Endpoints (relative to the API base) |
| -------------- | ------------------------------------------------------ | -------------------------------------------------------------------------------------------------- |
| `Guilds` | [`GuildManager`](Managers/GuildManager.cs) | `Discord/Guild` Query/Get/Upsert/Bulk Upsert; `…/{guildId}/GuildSettings` GET/PUT |
| `Users` | [`UserManager`](Managers/UserManager.cs) | `Discord/User` Query/Get/Upsert/Bulk Upsert |
| `Channels` | [`ChannelManager`](Managers/ChannelManager.cs) | `Discord/Channel` Query/Get/Upsert/Bulk Upsert |
| `Roles` | [`RoleManager`](Managers/RoleManager.cs) | `Discord/Role` Query/Get/Upsert/Bulk Upsert |
| `Emojis` | [`EmojiManager`](Managers/EmojiManager.cs) | `Discord/Emoji` Query/Get/Upsert/Bulk Upsert |
| `Interactions` | [`InteractionManager`](Managers/InteractionManager.cs) | `Discord/Interaction` Query/Get/Upsert/Bulk Upsert |
| `Messages` | [`MessageManager`](Managers/MessageManager.cs) | `Discord/Message` Query and Bulk Upsert |
| `Attachments` | [`AttachmentManager`](Managers/AttachmentManager.cs) | `Discord/Attachment` Query/Get/Upsert |
| `Members` | [`MemberManager`](Managers/MemberManager.cs) | `Discord/Member` Query/Upsert/Bulk Upsert/Patch (composite PK `(UserId, GuildId)` so no GET-by-id) |

Routes come from [`Lyo.Discord.Models.Constants.Rest.Discord`](../Lyo.Discord.Models/Constants.cs); request/response DTOs from
[`Lyo.Discord.Models`](../Lyo.Discord.Models/README.md). Guild-settings methods on `GuildManager` (`GetSettingsAsync`, `PutSettingsAsync`) hit the config-store routes mapped by
`MapDiscordGuildSettingsEndpoints`.

## Settings ([`LyoDiscordClientOptions`](LyoDiscordClientOptions.cs))

Binds from section `LyoDiscordClient` (overrides the base `ApiClient` section). Picks up every [`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flag (`BaseUrl`, `EnsureStatusCode`, `AcceptEncodings`, `RequestCompression`, …). `BaseUrl` defaults to `http://localhost:5251/` so a local Lyo API host is the target. Override for any other deployment.

## Wire into DI ([`Extensions.cs`](Extensions.cs))

| Method | Description |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `AddDiscordClientFromConfiguration(configuration, sectionName?)` | Reads `LyoDiscordClientOptions` from configuration (default section `"LyoDiscordClient"`) and registers `LyoDiscordClient` as a singleton. |
| `AddDiscordClient(Action<LyoDiscordClientOptions> configure)` | Constructs options in place. |
| `AddDiscordClient(LyoDiscordClientOptions options)` | Takes a ready-made options object. |

Every overload injects any registered `ILoggerFactory` and reuses a shared `HttpClient` from DI when one is present; otherwise the underlying `ApiClient` constructor builds its own
handler with auto-decompression enabled.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Discord.Models` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Microsoft.Extensions.Configuration` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)