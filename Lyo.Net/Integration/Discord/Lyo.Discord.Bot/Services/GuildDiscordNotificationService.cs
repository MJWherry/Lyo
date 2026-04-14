using DSharpPlus;
using DSharpPlus.Entities;
using Lyo.Cache;
using Microsoft.Extensions.Logging;

namespace Lyo.Discord.Bot.Services;

/// <inheritdoc />
public sealed class GuildDiscordNotificationService(ICacheService cache, ILogger<GuildDiscordNotificationService> log) : IGuildDiscordNotificationService
{
    /// <inheritdoc />
    public Task<bool> TrySendEmbedAsync(DiscordClient client, ulong guildId, ulong channelId, DiscordEmbed embed, CancellationToken cancellationToken = default)
        => TrySendMessageAsync(client, guildId, channelId, null, embed, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TrySendMessageAsync(
        DiscordClient client,
        ulong guildId,
        ulong channelId,
        string? content = null,
        DiscordEmbed? embed = null,
        CancellationToken cancellationToken = default)
    {
        var ch = TryResolveTextChannel(client, guildId, channelId);
        if (ch == null)
            return false;

        try {
            await ch.SendMessageAsync(content, embed).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) {
            log.LogWarning(ex, "Could not send message to channel {ChannelId} in guild {GuildId}", channelId, guildId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TrySendMessageAsync(DiscordClient client, ulong guildId, ulong channelId, DiscordMessageBuilder message, CancellationToken cancellationToken = default)
    {
        var ch = TryResolveTextChannel(client, guildId, channelId);
        if (ch == null)
            return false;

        try {
            await ch.SendMessageAsync(message).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) {
            log.LogWarning(ex, "Could not send message to channel {ChannelId} in guild {GuildId}", channelId, guildId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TrySendEmbedToGuildLogChannelAsync(DiscordClient client, ulong guildId, DiscordEmbed embed, CancellationToken cancellationToken = default)
    {
        if (!TryResolveLogChannelId(guildId, out var logChId))
            return false;

        return await TrySendEmbedAsync(client, guildId, logChId, embed, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TrySendMessageToGuildLogChannelAsync(
        DiscordClient client,
        ulong guildId,
        string? content = null,
        DiscordEmbed? embed = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveLogChannelId(guildId, out var logChId))
            return false;

        return await TrySendMessageAsync(client, guildId, logChId, content, embed, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> TrySendMessageToGuildLogChannelAsync(DiscordClient client, ulong guildId, DiscordMessageBuilder message, CancellationToken cancellationToken = default)
    {
        if (!TryResolveLogChannelId(guildId, out var logChId))
            return false;

        return await TrySendMessageAsync(client, guildId, logChId, message, cancellationToken).ConfigureAwait(false);
    }

    private bool TryResolveLogChannelId(ulong guildId, out ulong logChannelId)
    {
        logChannelId = 0;
        var gid = (long)guildId;
        var settings = cache.TryGetGuildSettings(gid);
        if (settings?.LogChannelId is not ulong id) {
            log.LogDebug("No log channel configured for guild {GuildId}", guildId);
            return false;
        }

        logChannelId = id;
        return true;
    }

    private DiscordChannel? TryResolveTextChannel(DiscordClient client, ulong guildId, ulong channelId)
    {
        if (!client.Guilds.TryGetValue(guildId, out var guild)) {
            log.LogDebug("Guild {GuildId} not in client cache; skip notification", guildId);
            return null;
        }

        var ch = guild.GetChannel(channelId);
        if (ch == null || !IsSendableTextChannel(ch)) {
            log.LogDebug("Channel {ChannelId} missing or not text in guild {GuildId}", channelId, guildId);
            return null;
        }

        return ch;
    }

    private static bool IsSendableTextChannel(DiscordChannel ch) => ch.Type is ChannelType.Text or ChannelType.News or ChannelType.PublicThread or ChannelType.PrivateThread;
}