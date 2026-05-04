using DSharpPlus.Entities;

namespace Lyo.Discord.Bot.Services;

/// <summary>Gateway messaging while the Discord bot is connected. Prefer this over taking a raw <see cref="DSharpPlus.DiscordClient" /> from DI.</summary>
public interface ILyoDiscordBotGateway
{
    bool IsConnected { get; }

    Task NotifyGuildLogMessageAsync(ulong guildId, string title, string body, CancellationToken ct = default);

    Task NotifyGuildLogErrorAsync(ulong guildId, Exception exception, string context, CancellationToken ct = default);

    Task<bool> TrySendEmbedAsync(ulong guildId, ulong channelId, DiscordEmbed embed, CancellationToken ct = default);

    Task<bool> TrySendMessageAsync(ulong guildId, ulong channelId, string? content = null, DiscordEmbed? embed = null, CancellationToken ct = default);

    Task<bool> TrySendMessageAsync(ulong guildId, ulong channelId, DiscordMessageBuilder message, CancellationToken ct = default);

    Task<bool> TrySendEmbedToGuildLogChannelAsync(ulong guildId, DiscordEmbed embed, CancellationToken ct = default);

    Task<bool> TrySendMessageToGuildLogChannelAsync(ulong guildId, string? content = null, DiscordEmbed? embed = null, CancellationToken ct = default);

    Task<bool> TrySendMessageToGuildLogChannelAsync(ulong guildId, DiscordMessageBuilder message, CancellationToken ct = default);
}