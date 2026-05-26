# Lyo.Discord.Client

Typed HTTP client for the Discord REST surface exposed by `Lyo.Api` (the `Discord/*` group registered by [`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md)). Wraps
`Lyo.Api.Client.ApiClient` so all Lyo-API behavior — Accept-Encoding, request compression, problem-details parsing — comes for free.

## Surface

[`LyoDiscordClient`](LyoDiscordClient.cs) is the entry point. It subclasses `ApiClient` and exposes nine **manager** properties under `Managers/`:

| Property        | Manager                                                        | Endpoints (relative to API base)                                            |
|-----------------|----------------------------------------------------------------|-----------------------------------------------------------------------------|
| `Guilds`        | [`GuildManager`](Managers/GuildManager.cs)                     | `Discord/Guild` Query/Get/Upsert/Bulk Upsert; `…/{guildId}/GuildSettings` GET/PUT |
| `Users`         | [`UserManager`](Managers/UserManager.cs)                       | `Discord/User` Query/Get/Upsert/Bulk Upsert                                  |
| `Channels`      | [`ChannelManager`](Managers/ChannelManager.cs)                 | `Discord/Channel` Query/Get/Upsert/Bulk Upsert                               |
| `Roles`         | [`RoleManager`](Managers/RoleManager.cs)                       | `Discord/Role` Query/Get/Upsert/Bulk Upsert                                  |
| `Emojis`        | [`EmojiManager`](Managers/EmojiManager.cs)                     | `Discord/Emoji` Query/Get/Upsert/Bulk Upsert                                 |
| `Interactions`  | [`InteractionManager`](Managers/InteractionManager.cs)         | `Discord/Interaction` Query/Get/Upsert/Bulk Upsert                           |
| `Messages`      | [`MessageManager`](Managers/MessageManager.cs)                 | `Discord/Message` Query and Bulk Upsert                                      |
| `Attachments`   | [`AttachmentManager`](Managers/AttachmentManager.cs)           | `Discord/Attachment` Query/Get/Upsert                                        |
| `Members`       | [`MemberManager`](Managers/MemberManager.cs)                   | `Discord/Member` Query/Upsert/Bulk Upsert/Patch (composite PK `(UserId, GuildId)` so no GET-by-id) |

Routes come from [`Lyo.Discord.Models.Constants.Rest.Discord`](../Lyo.Discord.Models/Constants.cs); request/response DTOs from
[`Lyo.Discord.Models`](../Lyo.Discord.Models/README.md). Guild-settings methods on `GuildManager` (`GetSettingsAsync`, `PutSettingsAsync`) hit the config-store routes mapped by
`MapDiscordGuildSettingsEndpoints`.

## Options ([`LyoDiscordClientOptions`](LyoDiscordClientOptions.cs))

Configuration section: `LyoDiscordClient` (shadows the base `ApiClient` section). Inherits all
[`ApiClientOptions`](../../Api/Lyo.Api.Client/README.md#options-apiclientoptions) flags (`BaseUrl`, `EnsureStatusCode`, `AcceptEncodings`, `RequestCompression`, …). Default
`BaseUrl` is `http://localhost:5251/` so it works against a locally-hosted Lyo API host out of the box; override for any other deployment.

## DI registration ([`Extensions.cs`](Extensions.cs))

| Method                                                          | Description                                                                                                                       |
|-----------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------|
| `AddDiscordClientFromConfiguration(configuration, sectionName?)`| Binds `LyoDiscordClientOptions` from configuration (default section `"LyoDiscordClient"`) and registers `LyoDiscordClient` singleton. |
| `AddDiscordClient(Action<LyoDiscordClientOptions> configure)`   | Builds options inline.                                                                                                            |
| `AddDiscordClient(LyoDiscordClientOptions options)`             | Registers a pre-built options instance.                                                                                           |

All overloads inject any registered `ILoggerFactory` and reuse a shared `HttpClient` from DI when present; otherwise the underlying `ApiClient` constructor builds its own
handler with auto-decompression enabled.

```csharp
services.AddDiscordClientFromConfiguration(builder.Configuration);

// or:
services.AddDiscordClient(o => {
    o.BaseUrl = "https://api.example.com/";
    o.RequestCompression = ApiRequestCompressionType.Gzip;
});
```

## Related projects

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md) — base HTTP client.
- [`Lyo.Api.Models`](../../Api/Lyo.Api.Models/README.md) — `QueryRes<T>`, `UpsertResult<T>` envelopes returned by managers.
- [`Lyo.Discord.Models`](../Lyo.Discord.Models/README.md) — request/response DTOs and shared route constants.
- [`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md) — the API host that exposes the `Discord/*` routes.
- [`Lyo.Query.Models`](../../../Data/Query/Lyo.Query.Models/README.md) — `QueryReq` filter shape.
