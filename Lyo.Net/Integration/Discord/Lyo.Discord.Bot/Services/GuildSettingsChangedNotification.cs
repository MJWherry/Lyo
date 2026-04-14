using Lyo.Discord.Models;
using Lyo.Notification;

namespace Lyo.Discord.Bot.Services;

/// <summary>Published when guild bot settings (command channel, log channel, roles) change after a cache update.</summary>
public sealed class GuildSettingsChangedNotification : INotification
{
    public long GuildId { get; }

    public DiscordGuildSettings Previous { get; }

    public DiscordGuildSettings Current { get; }

    public string Source { get; }

    public GuildSettingsChangedNotification(long guildId, DiscordGuildSettings previous, DiscordGuildSettings current, string source)
    {
        GuildId = guildId;
        Previous = previous;
        Current = current;
        Source = source;
    }
}