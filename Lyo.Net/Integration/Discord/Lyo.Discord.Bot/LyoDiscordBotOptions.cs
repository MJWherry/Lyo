using System.Diagnostics;
using DSharpPlus;

namespace Lyo.Discord.Bot;

/// <summary>
/// Settings for <see cref="LyoDiscordBotBase" /> (Discord token and gateway intents only). The Lyo API HTTP client is configured separately via
/// <see cref="Lyo.Discord.Client.LyoDiscordClientOptions" />.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class LyoDiscordBotOptions
{
    /// <summary>Default options-section name (matches existing appsettings: <c>DiscordBot</c>).</summary>
    public const string SectionName = "DiscordBot";

    /// <summary>Bot token (Bot scope). Leave empty to skip starting the bot in the host app.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>DSharpPlus gateway intents for the DSharpPlus client. Default: guilds + members (required for sync).</summary>
    public DiscordIntents Intents { get; set; } = DiscordIntents.Guilds | DiscordIntents.GuildMembers;

    /// <summary>Gateway intents and whether a bot token is set. The token is omitted.</summary>
    public override string ToString() => $"Intents={Intents} TokenSet={!string.IsNullOrWhiteSpace(Token)}";
}