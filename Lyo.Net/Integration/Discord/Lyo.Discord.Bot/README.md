# Lyo.Discord.Bot

Library (not an executable) that runs a DSharpPlus Discord bot and upserts guild data into your Lyo API (`Lyo.Discord.Client` against PostgreSQL-backed `Discord/*` endpoints). Host apps (for example `Lyo.TestConsole`) treat it as a base: configure the Discord token under `DiscordBot`, the Lyo API HTTP client under `LyoDiscordClient`, resolve services from DI, and call `RunAsync`.

## Features

- `LyoDiscordBotOptions` (bound from `DiscordBot`)
- `LyoDiscordClientOptions` (bound from `LyoDiscordClient`) and `Lyo.Discord.Client.LyoDiscordClient` (HTTP client used for upserts)
- `IGuildDatabaseSyncService` / `GuildDatabaseSyncService`
- `LyoDiscordBot` (singleton) and `LyoDiscordBotBase` (the same instance)

## Examples

### Add the bot

```csharp
using Lyo.Discord.Bot;

services.AddLyoDiscordBot<LyoDiscordBot>(configuration);
```

### Run the bot from a host

```csharp
var bot = host.Services.GetRequiredService<LyoDiscordBot>();
await bot.RunAsync(cancellationToken);
```

## Settings

`DiscordBot` (→ `LyoDiscordBotOptions`): settings that apply only to Discord.

| Property | Description |
| --------- | ---------------------------------------------------- |
| `Token` | Token for the Discord bot. |
| `Intents` | Optional. Gateway intent flags; default is `Guilds \ |

`LyoDiscordClient` (→ [`LyoDiscordClientOptions`](../Lyo.Discord.Client/LyoDiscordClientOptions.cs)): HTTP client for the Lyo API (`Discord/*` routes). Inherits `ApiClientOptions`. Set `BaseUrl` (defaults to `http://localhost:5092/` when omitted), plus compression, `AcceptEncodings`, `EnsureStatusCode`, etc.

## Service registration

- `LyoDiscordBotOptions` (bound from `DiscordBot`)
- `LyoDiscordClientOptions` (bound from `LyoDiscordClient`) and `Lyo.Discord.Client.LyoDiscordClient` (HTTP client used for upserts)
- `IGuildDatabaseSyncService` / `GuildDatabaseSyncService`
- `LyoDiscordBot` (singleton) and `LyoDiscordBotBase` (the same instance)

## Sync coverage

- **Full guild sync** (owner user if needed, guild row, channels bulk, emojis via REST + bulk, users + members bulk): `GuildAvailable`, `GuildCreated`, `GuildDownloadCompleted` (each guild in the download batch).
- **Guild metadata only**: `GuildUpdated`.
- **Single channel**: `ChannelCreated`, `ChannelUpdated`.
- **User + member row**: `GuildMemberAdded`, `GuildMemberUpdated`.
- **Emojis**: `GuildEmojisUpdated` (re-fetch via REST where applicable).

## Slash-command tree

A built-in slash-command tree under [`Commands/Settings/`](Commands/Settings) drives per-guild bot configuration through the Lyo API and the
`DiscordGuildSettings` config-store document. Discord/DSharpPlus will not let a slash group mix direct subcommands and nested subgroups, so everything hangs off subgroups:

| Command | Description |
| -------------------------------------- | ---------------------------------------------------------------------------------- |
| `/settings channels setcommandchannel` | Pick the channel where the bot accepts commands (defaults to the current channel). |
| `/settings channels setlogchannel` | Pick the channel where the bot posts errors and operational notices. |
| `/settings roles setmodrole` | Pick the moderator role used by bot permission checks. |
| `/settings roles setadminrole` | Pick the admin role used by bot permission checks. |
| `/settings info …` | Show effective guild settings (subgroup defined in `GuildSlashSettings`). |

Shared name/description constants live in [`GuildSlashSettings.cs`](Commands/Settings/GuildSlashSettings.cs); error responses are normalized via
[`SlashCommandErrorResponder`](Commands/SlashCommandErrorResponder.cs) and `DiscordCommandException`. Register the command module on your DSharpPlus client in
`ConfigureDiscordClient` (for example via `client.UseSlashCommands().RegisterCommands<GuildSettingsSlashCommands>(guildId)`).

Slash command handlers update settings through `LyoApi.Guilds.GetSettingsAsync` / `PutSettingsAsync` and then push the new document into the local cache (`Cache.SetGuildSettings`)
so the in-process bot reads the latest values without another API round-trip.

## Subclassing the bot

Subclass `LyoDiscordBotBase` and register your type with `AddLyoDiscordBot<MyBot>(configuration)`.

- **`ConfigureDiscordConfiguration`.** tweak `DiscordConfiguration` (for example intents).
- **`ConfigureDiscordClient`.** register DSharpPlus extensions (for example [CommandsNext](https://github.com/DSharpPlus/DSharpPlus), slash commands, interactivity). Call **before**
 handlers are wired.
- **`RegisterDefaultSyncHandlers`.** override if you need to change sync behavior; call `base.RegisterDefaultSyncHandlers(client)` to keep database sync.
- **`RegisterAdditionalHandlers`.** subscribe to other gateway events.

Example sketch:

```csharp
public sealed class MyBot : LyoDiscordBotBase
{
    public MyBot(LyoDiscordBotOptions o, Lyo.Discord.Client.LyoDiscordClient api, IGuildDatabaseSyncService s, ILoggerFactory lf)
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

## Package references

- **DSharpPlus**. gateway and REST helpers the bot host uses.
- **Lyo.Discord.Client**. HTTP upserts to your Lyo API.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Cache` (direct, lyo)
- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Diff` (direct, lyo)
- `Lyo.Discord.Client` (direct, lyo)
- `Lyo.Notification` (direct, lyo)
- `DSharpPlus` `4.5.2` (direct, third-party)
- `DSharpPlus.CommandsNext` `4.5.2` (direct, third-party)
- `DSharpPlus.Interactivity` `4.5.2` (direct, third-party)
- `DSharpPlus.SlashCommands` `4.5.2` (direct, third-party)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Common.Json` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Discord.Models` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Http.Client` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Http` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)