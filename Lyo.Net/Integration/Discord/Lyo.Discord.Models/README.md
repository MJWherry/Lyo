# Lyo.Discord.Models

Wire-level DTOs and shared constants for the Discord integration. Used by [`Lyo.Discord.Client`](../Lyo.Discord.Client/README.md) (typed HTTP client) and [`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md) (API host + persistence) so request and response shapes match.

## Request DTOs ([`Request/`](Request))

Mutable request DTOs that the API host maps to its EF entities (audit timestamps are set by CRUD hooks, not by clients). One per Discord domain entity: `DiscordUserReq`, `DiscordGuildReq`, `DiscordChannelReq`, `DiscordEmojiReq`, `DiscordRoleReq`, `DiscordInteractionReq`, `DiscordMessageReq`, `DiscordAttachmentReq`, `DiscordMemberReq`.

## Response DTOs ([`Response/`](Response))

Sealed `record`s describing read shapes (typically a subset of entity columns excluding audit timestamps): `DiscordUserRes`, `DiscordGuildRes`, `DiscordChannelRes`, `DiscordEmojiRes`, `DiscordRoleRes`, `DiscordInteractionRes`, `DiscordMessageRes`, `DiscordAttachmentRes`, `DiscordMemberRes`.

## Per-guild settings ([`DiscordGuildSettings.cs`](DiscordGuildSettings.cs))

`DiscordGuildSettings` is the document persisted by the config store under `EntityRef.For<DiscordGuild>(guildId)`. Versioned for forward-compatibility:

| Property | Description |
| ---------------------- | ------------------------------------------------------------------------------------------------------- |
| `Version` | Schema version (`CurrentSchemaVersion = 3`); bumped via `NormalizeForRead` / `NormalizeForPersistence`. |
| `CommandChannelId` | Channel where the bot accepts commands (`null` = no restriction by channel). |
| `LogChannelId` | Channel where the bot posts errors and operational notices (`null` = disabled). |
| `AdminRoleId` | Role treated as server admin for bot permission checks. |
| `ModRoleId` | Role treated as moderator for bot permission checks. |
| `Revision` | Monotonic config-binding revision; populated on read, cleared before write so it is never persisted. |
| `ComicReaderChannelId` | When set, comic reading is restricted to this channel. |
| `ComicDefaultLanguage` | Optional BCP-47 language filter for comic search and chapter lists. |
| `ComicNsfwAllowed` | When false (default), comic reading is rejected in NSFW channels. |

`NormalizeForRead()` upgrades older snapshots in-place (e.g. v1 → v2 → v3). `NormalizeForPersistence()` calls `NormalizeForRead()` and stamps `Version = CurrentSchemaVersion`
before save.

## Route constants ([`Constants.cs`](Constants.cs))

`Constants.Rest.Discord` holds route segments shared by client managers and host endpoints:

| Constant / helper | Value |
| ------------------------------------------------------------------------------------------------------ | --------------------------------------------------------- |
| `Route` | `"Discord"` |
| `Users`, `Guilds`, `Channels`, `Emojis`, `Roles`, `Interactions`, `Messages`, `Attachments`, `Members` | `"Discord/User"`, `"Discord/Guild"`, etc. |
| `GuildSettings(guildId)` | `Discord/Guild/{guildId}/GuildSettings` |
| `GuildSettingsRevisions(guildId)` | `Discord/Guild/{guildId}/GuildSettings/Revisions` |
| `GuildSettingsRevert(guildId, revision)` | `Discord/Guild/{guildId}/GuildSettings/Revert/{revision}` |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Models` (direct, lyo)
- `Lyo.Common` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)