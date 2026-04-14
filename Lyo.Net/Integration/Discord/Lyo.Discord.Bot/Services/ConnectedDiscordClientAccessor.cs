using DSharpPlus;

namespace Lyo.Discord.Bot.Services;

/// <summary>Holds the connected gateway <see cref="DiscordClient" /> while the bot is running so services can post to guild channels (e.g. settings change logs).</summary>
public sealed class ConnectedDiscordClientAccessor
{
    /// <summary>The active client, or null when the bot is not connected.</summary>
    public DiscordClient? Client { get; set; }
}