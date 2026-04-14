namespace Lyo.Discord.Models.Response;

public sealed record DiscordEmojiRes(
    long Id,
    long? GuildId,
    string Name,
    string? Url,
    bool IsAnimated,
    bool IsAvailable,
    bool IsManaged,
    bool RequiresColons,
    DateTime EmojiCreatedDate);