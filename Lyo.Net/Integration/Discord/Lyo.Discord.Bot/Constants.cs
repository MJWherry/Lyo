namespace Lyo.Discord.Bot;

/// <summary>Cache key prefix and tag for guild bot settings in <see cref="Lyo.Cache.ICacheService" />.</summary>
public static class Constants
{
    public static class Cache
    {
        /// <summary>Key prefix for per-guild settings: <c>discord:guildsettings:{guildId}</c> (full key is lowercased).</summary>
        public const string GuildSettingsPrefix = "discord:guildsettings:";

        /// <summary>All guild settings entries tag passed to cache invalidation.</summary>
        public const string GuildSettingsTag = "discord:guildsettings";

        /// <summary>Cache entry key for <see cref="Lyo.Discord.Models.DiscordGuildSettings" /> for <paramref name="guildId" />.</summary>
        public static string GuildSettingsKey(long guildId) => $"{GuildSettingsPrefix}{guildId}".ToLowerInvariant();
    }
}