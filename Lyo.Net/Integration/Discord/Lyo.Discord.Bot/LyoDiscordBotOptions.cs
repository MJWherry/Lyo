using DSharpPlus;

namespace Lyo.Discord.Bot;

/// <summary>Configuration for <see cref="LyoDiscordBotBase" /> (bot token, Lyo API URL, gateway intents).</summary>
public sealed class LyoDiscordBotOptions
{
    /// <summary>Default configuration section name (matches existing appsettings: <c>DiscordBot</c>).</summary>
    public const string SectionName = "DiscordBot";

    /// <summary>Discord bot token (Bot scope). Leave empty to skip starting the bot in the host app.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Base URL of the Lyo API hosting <c>Discord/*</c> endpoints (trailing slash optional).</summary>
    public string LyoApiBaseUrl { get; set; } = "http://localhost:5092/";

    /// <summary>Gateway intents for the DSharpPlus client. Default: guilds + members (required for sync).</summary>
    public DiscordIntents Intents { get; set; } = DiscordIntents.Guilds | DiscordIntents.GuildMembers;
}