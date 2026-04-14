namespace Lyo.Discord.Bot;

/// <summary>Key prefix and tag for guild bot settings in <see cref="Lyo.Cache.ICacheService" />.</summary>
public static class Constants
{
    public static class Cache
    {
        /// <summary>Prefix for per-guild settings entries: <c>discord:guildsettings:{guildId}</c> (full key is lowercased).</summary>
        public const string GuildSettingsPrefix = "discord:guildsettings:";

        /// <summary>Tag passed to cache invalidation for all guild settings entries.</summary>
        public const string GuildSettingsTag = "discord:guildsettings";

        /// <summary>Cache key for <see cref="Lyo.Discord.Models.DiscordGuildSettings" /> for <paramref name="guildId" />.</summary>
        public static string GuildSettingsKey(long guildId) => $"{GuildSettingsPrefix}{guildId}".ToLowerInvariant();
    }
}