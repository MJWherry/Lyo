using DSharpPlus.Entities;

namespace Lyo.Discord.Bot.Services;

/// <summary>Upserts Discord entities exposed by the Lyo API so other apps can read a consistent database snapshot.</summary>
public interface IGuildDatabaseSyncService
{
    /// <summary>Full sync: owner user (if needed), guild, channels (bulk), emojis (bulk), users + members (bulk).</summary>
    Task SyncGuildFullAsync(DiscordGuild guild, CancellationToken ct = default);

    /// <summary>Guild row only (lighter; use on GuildUpdated).</summary>
    Task SyncGuildMetadataAsync(DiscordGuild guild, CancellationToken ct = default);

    /// <summary>Single channel upsert.</summary>
    Task SyncChannelAsync(DiscordChannel channel, CancellationToken ct = default);

    /// <summary>User + membership row for one member.</summary>
    Task SyncGuildMemberAsync(DiscordMember member, CancellationToken ct = default);

    /// <summary>Re-fetch emojis from Discord REST and bulk upsert.</summary>
    Task SyncGuildEmojisAsync(DiscordGuild guild, CancellationToken ct = default);
}