using DSharpPlus.Entities;

namespace Lyo.Discord.Bot.Services;

/// <summary>Writes Discord entities exposed by the Lyo API so other apps can read a consistent database snapshot.</summary>
public interface IGuildDatabaseSyncService
{
    /// <summary>Full sync: owner user when needed, then guild, channels, emojis, users, and members in bulk.</summary>
    Task SyncGuildFullAsync(DiscordGuild guild, CancellationToken ct = default);

    /// <summary>Guild row only (cheaper; use on GuildUpdated).</summary>
    Task SyncGuildMetadataAsync(DiscordGuild guild, CancellationToken ct = default);

    /// <summary>Upsert one channel.</summary>
    Task SyncChannelAsync(DiscordChannel channel, CancellationToken ct = default);

    /// <summary>One member user + membership row.</summary>
    Task SyncGuildMemberAsync(DiscordMember member, CancellationToken ct = default);

    /// <summary>Reload emojis from Discord REST and upsert in bulk.</summary>
    Task SyncGuildEmojisAsync(DiscordGuild guild, CancellationToken ct = default);
}