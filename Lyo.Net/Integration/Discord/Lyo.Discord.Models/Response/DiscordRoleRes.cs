namespace Lyo.Discord.Models.Response;

public sealed record DiscordRoleRes(
    long Id,
    long GuildId,
    long? EmojiId,
    string Name,
    string? Icon,
    string Color,
    bool IsHoisted,
    bool IsManaged,
    bool IsMentionable,
    int Position,
    DateTime RoleCreatedDate);