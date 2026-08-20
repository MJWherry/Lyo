# Lyo.Discord.Client

Typed HTTP client for the Discord REST endpoints exposed by `Lyo.Api` (the `Discord/*` group registered by [`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md)). Wraps `Lyo.Api.Client.ApiClient` so Accept-Encoding, request compression, and problem-details parsing match other Lyo clients.

## Examples

### DI registration ([`Extensions.cs`](Extensions.cs))

```csharp
services.AddDiscordClientFromConfiguration(builder.Configuration);

// or:
services.AddDiscordClient(o => {
    o.BaseUrl = "https://api.example.com/";
    o.RequestCompression = ApiRequestCompressionType.Gzip;
});
```

## Managers

[`LyoDiscordClient`](LyoDiscordClient.cs) is the entry point. It subclasses `ApiClient` and exposes nine **manager** properties under `Managers/`:

| Property | Manager | Endpoints (relative to API base) |
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

## Options ([`LyoDiscordClientOptions`](LyoDiscordClientOptions.cs))

Configuration section: `LyoDiscordClient` (shadows the base `ApiClient` section). Inherits all [`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flags (`BaseUrl`, `EnsureStatusCode`, `AcceptEncodings`, `RequestCompression`, …). Default `BaseUrl` is `http://localhost:5251/` so it targets a local Lyo API host. Override for any other deployment.

## DI registration ([`Extensions.cs`](Extensions.cs))

| Method | Description |
| ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| `AddDiscordClientFromConfiguration(configuration, sectionName?)` | Binds `LyoDiscordClientOptions` from configuration (default section `"LyoDiscordClient"`) and registers `LyoDiscordClient` singleton. |
| `AddDiscordClient(Action<LyoDiscordClientOptions> configure)` | Builds options inline. |
| `AddDiscordClient(LyoDiscordClientOptions options)` | Registers a pre-built options instance. |

All overloads inject any registered `ILoggerFactory` and reuse a shared `HttpClient` from DI when present; otherwise the underlying `ApiClient` constructor builds its own
handler with auto-decompression enabled.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` (direct, lyo)
- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Discord.Models` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft)