using DSharpPlus;
using DSharpPlus.Entities;
using Lyo.Cache;
using Lyo.Diff;
using Lyo.Discord.Bot.Services;
using Lyo.Discord.Models;
using Lyo.Notification;

namespace Lyo.Discord.Bot;

/// <summary>Typed read/write for guild settings on <see cref="ICacheService" />, including <see cref="GuildSettingsChangedNotification" /> on meaningful updates.</summary>
public static class Extensions
{
    private const int MaxEmbedDescription = 3900;
    private static readonly TimeSpan GuildSettingsTtl = TimeSpan.FromDays(1);

    private static string Truncate(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLen)
            return s;

        return s.Substring(0, maxLen) + "…";
    }

    extension(ICacheService cache)
    {
        /// <summary>Returns cached settings for <paramref name="guildId" />, or null if missing or expired.</summary>
        public DiscordGuildSettings? TryGetGuildSettings(long guildId)
        {
            var key = Constants.Cache.GuildSettingsKey(guildId);
            return cache.TryGetValue(key, out DiscordGuildSettings? s) ? s : null;
        }

        /// <summary>Replaces the cache entry and publishes <see cref="GuildSettingsChangedNotification" /> when meaningful values change.</summary>
        public void SetGuildSettings(IDiffService diffService, INotificationPublisher notificationPublisher, long guildId, DiscordGuildSettings settings, string source)
        {
            var key = Constants.Cache.GuildSettingsKey(guildId);
            var previous = cache.TryGetGuildSettings(guildId);
            settings.NormalizeForRead();
            cache.InvalidateCacheItem(key).GetAwaiter().GetResult();
            cache.GetOrSet(key, settings, o => o.SetDuration(GuildSettingsTtl), [Constants.Cache.GuildSettingsTag]);
            if (previous != null && diffService.Objects.GetDifferences(previous, settings).Count > 0)
                _ = notificationPublisher.PublishAsync(new GuildSettingsChangedNotification(guildId, previous, settings, source), CancellationToken.None);
        }
    }

    extension(IGuildDiscordNotificationService notifications)
    {
        /// <summary>Posts an error-style embed to the configured guild log channel.</summary>
        public Task NotifyGuildLogErrorAsync(DiscordClient client, ulong guildId, Exception exception, string context, CancellationToken cancellationToken = default)
        {
            var text = Truncate(exception.ToString(), MaxEmbedDescription);
            var embed = new DiscordEmbedBuilder().WithTitle("Bot error")
                .WithColor(DiscordColor.Orange)
                .AddField("Context", Truncate(context, 1024))
                .WithDescription(text)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            return notifications.TrySendEmbedToGuildLogChannelAsync(client, guildId, embed, cancellationToken);
        }

        /// <summary>Posts an informational embed to the configured guild log channel.</summary>
        public Task NotifyGuildLogMessageAsync(DiscordClient client, ulong guildId, string title, string body, CancellationToken cancellationToken = default)
        {
            var embed = new DiscordEmbedBuilder().WithTitle(Truncate(title, 256))
                .WithColor(new(0x5865F2))
                .WithDescription(Truncate(body, MaxEmbedDescription))
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            return notifications.TrySendEmbedToGuildLogChannelAsync(client, guildId, embed, cancellationToken);
        }
    }
}