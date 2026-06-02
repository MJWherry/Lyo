using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordEmojiRes(
    long Id,
    long? GuildId,
    string Name,
    string? Url,
    bool IsAnimated,
    bool IsAvailable,
    bool IsManaged,
    bool RequiresColons,
    DateTime EmojiCreatedDate)
{
    public override string ToString() => $"DiscordEmojiRes: :{Name}: id={Id}, animated={IsAnimated}";
}