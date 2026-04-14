using DSharpPlus;
using DSharpPlus.Entities;
using Lyo.Discord.Models;

namespace Lyo.Discord.Bot.Services;

/// <summary>
/// Sends messages to Discord guild channels: any channel by id, or the guild log channel from cached <see cref="DiscordGuildSettings" /> (via
/// <see cref="Lyo.Cache.ICacheService" />).
/// </summary>
public interface IGuildDiscordNotificationService
{
    /// <summary>Sends an embed to a specific channel. Returns false if the guild is missing, the channel is missing or not text-based, or send failed.</summary>
    Task<bool> TrySendEmbedAsync(DiscordClient client, ulong guildId, ulong channelId, DiscordEmbed embed, CancellationToken cancellationToken = default);

    /// <summary>Sends plain content and/or an embed to a specific channel.</summary>
    Task<bool> TrySendMessageAsync(
        DiscordClient client,
        ulong guildId,
        ulong channelId,
        string? content = null,
        DiscordEmbed? embed = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a <see cref="DiscordMessageBuilder" /> (files, components, multiple embeds, etc.) to a specific channel.</summary>
    Task<bool> TrySendMessageAsync(DiscordClient client, ulong guildId, ulong channelId, DiscordMessageBuilder message, CancellationToken cancellationToken = default);

    /// <summary>Resolves the configured log channel for the guild and sends the embed.</summary>
    Task<bool> TrySendEmbedToGuildLogChannelAsync(DiscordClient client, ulong guildId, DiscordEmbed embed, CancellationToken cancellationToken = default);

    /// <summary>Resolves the configured log channel for the guild and sends content and/or an embed.</summary>
    Task<bool> TrySendMessageToGuildLogChannelAsync(
        DiscordClient client,
        ulong guildId,
        string? content = null,
        DiscordEmbed? embed = null,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves the configured log channel for the guild and sends a built message.</summary>
    Task<bool> TrySendMessageToGuildLogChannelAsync(DiscordClient client, ulong guildId, DiscordMessageBuilder message, CancellationToken cancellationToken = default);
}