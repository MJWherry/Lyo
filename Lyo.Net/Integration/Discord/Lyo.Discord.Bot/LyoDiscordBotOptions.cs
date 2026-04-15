using DSharpPlus;

namespace Lyo.Discord.Bot;

/// <summary>Configuration for <see cref="LyoDiscordBotBase" /> (Discord token and gateway intents only). The Lyo API HTTP client is configured separately via <see cref="Lyo.Discord.Client.LyoDiscordClientOptions" />.</summary>
public sealed class LyoDiscordBotOptions
{
    /// <summary>Default configuration section name (matches existing appsettings: <c>DiscordBot</c>).</summary>
    public const string SectionName = "DiscordBot";

    /// <summary>Discord bot token (Bot scope). Leave empty to skip starting the bot in the host app.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gateway intents for the DSharpPlus client. Default: guilds + members (required for sync).</summary>
    public DiscordIntents Intents { get; set; } = DiscordIntents.Guilds | DiscordIntents.GuildMembers;
}