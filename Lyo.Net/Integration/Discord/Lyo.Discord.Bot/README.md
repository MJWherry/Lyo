# Lyo.Discord.Bot

Library (not an executable) that runs a DSharpPlus Discord bot and upserts guild data into your Lyo API (`Lyo.Discord.Client` to PostgreSQL-backed `Discord/*` endpoints). Use it as a base so host apps (e.g. `Lyo.TestConsole`) configure the Discord token under `DiscordBot`, the Lyo API HTTP client under `LyoDiscordClient`, resolve services from DI, and call `RunAsync`.

## Features

- `LyoDiscordBotOptions` (from `DiscordBot`)
- `LyoDiscordClientOptions` (from `LyoDiscordClient`) and `Lyo.Discord.Client.LyoDiscordClient` (HTTP client for upserts)
- `IGuildDatabaseSyncService` / `GuildDatabaseSyncService`
- `LyoDiscordBot` (singleton) and `LyoDiscordBotBase` (same instance)

## Examples

### Register services

```csharp
using Lyo.Discord.Bot;

services.AddLyoDiscordBot<LyoDiscordBot>(configuration);
```

### Starting the bot from a host app

```csharp
var bot = host.Services.GetRequiredService<LyoDiscordBot>();
await bot.RunAsync(cancellationToken);
```

## Configuration

`DiscordBot` (→ `LyoDiscordBotOptions`): Discord-only settings.

| Property | Description |
| --------- | ----------------------------------------------- |
| `Token` | Discord bot token. |
| `Intents` | Optional. Gateway intents; default is `Guilds \ |

`LyoDiscordClient` (→ [`LyoDiscordClientOptions`](../Lyo.Discord.Client/LyoDiscordClientOptions.cs)): HTTP client for the Lyo API (`Discord/*` routes). Inherits `ApiClientOptions`. Set `BaseUrl` (default `http://localhost:5092/` if omitted), plus compression, `AcceptEncodings`, `EnsureStatusCode`, etc.

## Registration

- `LyoDiscordBotOptions` (from `DiscordBot`)
- `LyoDiscordClientOptions` (from `LyoDiscordClient`) and `Lyo.Discord.Client.LyoDiscordClient` (HTTP client for upserts)
- `IGuildDatabaseSyncService` / `GuildDatabaseSyncService`
- `LyoDiscordBot` (singleton) and `LyoDiscordBotBase` (same instance)

## What gets synced

- **Full guild sync** (owner user if needed, guild row, channels bulk, emojis via REST + bulk, users + members bulk): `GuildAvailable`, `GuildCreated`, `GuildDownloadCompleted` ( each guild in the download batch).
- **Guild metadata only**: `GuildUpdated`.
- **Single channel**: `ChannelCreated`, `ChannelUpdated`.
- **User + member row**: `GuildMemberAdded`, `GuildMemberUpdated`.
- **Emojis**: `GuildEmojisUpdated` (re-fetch via REST where applicable).

## Slash commands

The package ships a built-in slash-command tree under [`Commands/Settings/`](Commands/Settings) that drives per-guild bot configuration through the Lyo API and the
`DiscordGuildSettings` config-store document. Discord/DSharpPlus do not allow a slash group to mix direct subcommands and nested subgroups, so everything hangs off subgroups:

| Command | Description |
| -------------------------------------- | ----------------------------------------------------------------------------- |
| `/settings channels setcommandchannel` | Set the channel where the bot accepts commands (defaults to current channel). |
| `/settings channels setlogchannel` | Set the channel where the bot posts errors and operational notices. |
| `/settings roles setmodrole` | Set the moderator role used by bot permission checks. |
| `/settings roles setadminrole` | Set the admin role used by bot permission checks. |
| `/settings info …` | Display effective guild settings (subgroup defined in `GuildSlashSettings`). |

Centralized name/description constants live in [`GuildSlashSettings.cs`](Commands/Settings/GuildSlashSettings.cs); error responses are normalized via
[`SlashCommandErrorResponder`](Commands/SlashCommandErrorResponder.cs) and `DiscordCommandException`. Register the command module on your DSharpPlus client in
`ConfigureDiscordClient` (e.g. via `client.UseSlashCommands().RegisterCommands<GuildSettingsSlashCommands>(guildId)`).

Slash command handlers update settings via `LyoApi.Guilds.GetSettingsAsync` / `PutSettingsAsync` and then push the new document into the local cache (`Cache.SetGuildSettings`)
so the in-process bot reads the latest values without an extra API round-trip.

## Extending the base bot

Derive from `LyoDiscordBotBase` and register your type with `AddLyoDiscordBot<MyBot>(configuration)`.

- **`ConfigureDiscordConfiguration`.** adjust `DiscordConfiguration` (e.g. intents).
- **`ConfigureDiscordClient`.** register DSharpPlus extensions (e.g. [CommandsNext](https://github.com/DSharpPlus/DSharpPlus), slash commands, interactivity). Call **before**
 handlers are wired.
- **`RegisterDefaultSyncHandlers`.** override if you need to change sync behavior; call `base.RegisterDefaultSyncHandlers(client)` to keep database sync.
- **`RegisterAdditionalHandlers`.** subscribe to other gateway events.

Example sketch:

```csharp
public sealed class MyBot : LyoDiscordBotBase
{
    public MyBot(IOptions<LyoDiscordBotOptions> o, Lyo.Discord.Client.LyoDiscordClient api, IGuildDatabaseSyncService s, ILoggerFactory lf)
        : base(o, api, s, lf) { }

    protected override void ConfigureDiscordClient(DSharpPlus.DiscordClient client)
    {
        // client.UseCommandsNext(...);
    }

    protected override void RegisterAdditionalHandlers(DSharpPlus.DiscordClient client)
    {
        client.MessageCreated += async (_, e) => { /* ... */ };
    }
}
```

## Packages

- **DSharpPlus**. gateway and REST helpers used by the bot host.
- **Lyo.Discord.Client**. HTTP upserts to your Lyo API.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` (direct, lyo)
- `Lyo.Common` (direct, lyo)
- `Lyo.Diff` (direct, lyo)
- `Lyo.Discord.Client` (direct, lyo)
- `Lyo.Notification` (direct, lyo)
- `DSharpPlus` `4.5.2` (direct, third-party)
- `DSharpPlus.CommandsNext` `4.5.2` (direct, third-party)
- `DSharpPlus.Interactivity` `4.5.2` (direct, third-party)
- `DSharpPlus.SlashCommands` `4.5.2` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Api.Client` (transitive, lyo)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Discord.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)