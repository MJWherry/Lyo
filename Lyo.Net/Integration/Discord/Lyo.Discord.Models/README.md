# Lyo.Discord.Models

Wire-level DTOs and shared constants for the Discord integration. Used by both [`Lyo.Discord.Client`](../Lyo.Discord.Client/README.md) (typed HTTP client) and
[`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md) (API host + persistence) so request/response shapes stay symmetric.

## Request DTOs ([`Request/`](Request))

Mutable request DTOs that the API host maps to its EF entities (audit timestamps are set by CRUD hooks, not by clients). One per Discord domain entity:

`DiscordUserReq`, `DiscordGuildReq`, `DiscordChannelReq`, `DiscordEmojiReq`, `DiscordRoleReq`, `DiscordInteractionReq`, `DiscordMessageReq`, `DiscordAttachmentReq`,
`DiscordMemberReq`.

## Response DTOs ([`Response/`](Response))

Sealed `record`s describing read shapes (typically a subset of entity columns excluding audit timestamps): `DiscordUserRes`, `DiscordGuildRes`, `DiscordChannelRes`,
`DiscordEmojiRes`, `DiscordRoleRes`, `DiscordInteractionRes`, `DiscordMessageRes`, `DiscordAttachmentRes`, `DiscordMemberRes`.

## Per-guild settings ([`DiscordGuildSettings.cs`](DiscordGuildSettings.cs))

`DiscordGuildSettings` is the document persisted by the config store under `EntityRef.For<DiscordGuild>(guildId)`. Versioned for forward-compatibility:

| Property           | Description                                                                                             |
|--------------------|---------------------------------------------------------------------------------------------------------|
| `Version`          | Schema version (`CurrentSchemaVersion = 2`); bumped via `NormalizeForRead` / `NormalizeForPersistence`. |
| `CommandChannelId` | Channel where the bot accepts commands (`null` = no restriction by channel).                            |
| `LogChannelId`     | Channel where the bot posts errors and operational notices (`null` = disabled).                         |
| `AdminRoleId`      | Role treated as server admin for bot permission checks.                                                 |
| `ModRoleId`        | Role treated as moderator for bot permission checks.                                                    |
| `Revision`         | Monotonic config-binding revision; populated on read, cleared before write so it is never persisted.    |

`NormalizeForRead()` upgrades older snapshots in-place (e.g. v1 → v2). `NormalizeForPersistence()` calls `NormalizeForRead()` and stamps `Version = CurrentSchemaVersion`
before save.

## Route constants ([`Constants.cs`](Constants.cs))

`Constants.Rest.Discord` centralizes route segments so client managers and host endpoints stay in lock-step:

| Constant / helper                                                                                      | Value                                                     |
|--------------------------------------------------------------------------------------------------------|-----------------------------------------------------------|
| `Route`                                                                                                | `"Discord"`                                               |
| `Users`, `Guilds`, `Channels`, `Emojis`, `Roles`, `Interactions`, `Messages`, `Attachments`, `Members` | `"Discord/User"`, `"Discord/Guild"`, etc.                 |
| `GuildSettings(guildId)`                                                                               | `Discord/Guild/{guildId}/GuildSettings`                   |
| `GuildSettingsRevisions(guildId)`                                                                      | `Discord/Guild/{guildId}/GuildSettings/Revisions`         |
| `GuildSettingsRevert(guildId, revision)`                                                               | `Discord/Guild/{guildId}/GuildSettings/Revert/{revision}` |

## Related projects

- [`Lyo.Api.Models`](../../Api/Lyo.Api.Models/README.md) — generic envelopes (`QueryRes<T>`, `UpsertResult<T>`, `LyoProblemDetails`) wrapping these DTOs.
- [`Lyo.Discord.Client`](../Lyo.Discord.Client/README.md) — typed managers that consume these DTOs.
- [`Lyo.Discord.Postgres`](../Lyo.Discord.Postgres/README.md) — persistence + API host.
- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)
