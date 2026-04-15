# Lyo.Discord.Bot

Library (not an executable) that runs a **DSharpPlus** Discord bot and **upserts** guild data into your Lyo API (`Lyo.Discord.Client` → PostgreSQL-backed `Discord/*` endpoints).
Use it as a **base** so host apps (e.g. `Lyo.TestConsole`) configure the Discord token under **`DiscordBot`**, the Lyo API HTTP client under **`LyoDiscordClient`**, resolve services from DI, and call `RunAsync`.

## Configuration

**`DiscordBot`** (→ `LyoDiscordBotOptions`): Discord-only settings.

| Property  | Description                                                                                                            |
|-----------|------------------------------------------------------------------------------------------------------------------------|
| `Token`   | Discord bot token.                                                                                                     |
| `Intents` | Optional. Gateway intents; default is `Guilds \| GuildMembers`. For JSON, use the numeric flags value Discord expects. |

**`LyoDiscordClient`** (→ [`LyoDiscordClientOptions`](../Lyo.Discord.Client/LyoDiscordClientOptions.cs)): HTTP client for the Lyo API (`Discord/*` routes). Inherits **`ApiClientOptions`** — set **`BaseUrl`** (default `http://localhost:5092/` if omitted), plus compression, **`AcceptEncodings`**, **`EnsureStatusCode`**, etc.

## Registration

```csharp
using Lyo.Discord.Bot;

services.AddLyoDiscordBot<LyoDiscordBot>(configuration);
```

This registers:

- `LyoDiscordBotOptions` (from **`DiscordBot`**)
- `LyoDiscordClientOptions` (from **`LyoDiscordClient`**) and `Lyo.Discord.Client.LyoDiscordClient` (HTTP client for upserts)
- `IGuildDatabaseSyncService` / `GuildDatabaseSyncService`
- `LyoDiscordBot` (singleton) and `LyoDiscordBotBase` (same instance)

## Starting the bot from a host app

```csharp
var bot = host.Services.GetRequiredService<LyoDiscordBot>();
await bot.RunAsync(cancellationToken);
```

Ensure `Token` is set; otherwise skip starting the bot.

## What gets synced

- **Full guild sync** (owner user if needed, guild row, channels bulk, emojis via REST + bulk, users + members bulk): `GuildAvailable`, `GuildCreated`, `GuildDownloadCompleted` (
  each guild in the download batch).
- **Guild metadata only**: `GuildUpdated`.
- **Single channel**: `ChannelCreated`, `ChannelUpdated`.
- **User + member row**: `GuildMemberAdded`, `GuildMemberUpdated`.
- **Emojis**: `GuildEmojisUpdated` (re-fetch via REST where applicable).

Logging uses `ILogger` with scopes and Information-level lines around upserts.

## Extending the base bot

Derive from `LyoDiscordBotBase` and register your type with `AddLyoDiscordBot<MyBot>(configuration)`.

- **`ConfigureDiscordConfiguration`** — adjust `DiscordConfiguration` (e.g. intents).
- **`ConfigureDiscordClient`** — register DSharpPlus extensions (e.g. [CommandsNext](https://github.com/DSharpPlus/DSharpPlus), slash commands, interactivity). Call **before**
  handlers are wired.
- **`RegisterDefaultSyncHandlers`** — override if you need to change sync behavior; call `base.RegisterDefaultSyncHandlers(client)` to keep database sync.
- **`RegisterAdditionalHandlers`** — subscribe to other gateway events.

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

- **DSharpPlus** — gateway and REST helpers used by the bot host.
- **Lyo.Discord.Client** — HTTP upserts to your Lyo API.

The database schema itself lives in **Lyo.Discord.Postgres**; this package only drives sync through the API.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Discord.Bot.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `DSharpPlus` | `4.5.1` |
| `DSharpPlus.CommandsNext` | `4.5.1` |
| `DSharpPlus.Interactivity` | `4.5.1` |
| `DSharpPlus.SlashCommands` | `4.5.1` |
| `Microsoft.Extensions.Caching.Memory` | `[10.0.1,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10.0.1,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10.0.1,)` |
| `Microsoft.Extensions.DependencyInjection` | `[10.0.1,)` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `[10.0.1,)` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10.0.1,)` |
| `Microsoft.Extensions.Options` | `[10.0.1,)` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `[10.0.1,)` |

### Project references

- `Lyo.Cache`
- `Lyo.Common`
- `Lyo.Diff`
- `Lyo.Discord.Client`
- `Lyo.Notification`

## Public API (generated)

Top-level `public` types in `*.cs` (*31*). Nested types and file-scoped namespaces may omit some entries.

- `Cache`
- `Channel`
- `Channels`
- `ConnectedDiscordClientAccessor`
- `Constants`
- `DiscordCommandException`
- `DiscordCommandHelpers`
- `Extensions`
- `GuildDatabaseSyncService`
- `GuildDiscordNotificationService`
- `GuildSettingsChangedNotification`
- `GuildSettingsChangedNotificationHandler`
- `GuildSettingsEmbedBuilder`
- `GuildSettingsSlashCommands`
- `GuildSlashSettings`
- `IGuildDatabaseSyncService`
- `IGuildDiscordNotificationService`
- `Info`
- `LyoDiscordBot`
- `LyoDiscordBotBase`
- `LyoDiscordBotOptions`
- `Role`
- `Roles`
- `ServiceCollectionExtensions`
- `SetAdminRole`
- `SetCommandChannel`
- `SetLogChannel`
- `SetModRole`
- `Settings`
- `Show`
- `SlashCommandErrorResponder`

<!-- LYO_README_SYNC:END -->

