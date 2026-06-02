using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
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
    DateTime RoleCreatedDate)
{
    public override string ToString()
        => $"DiscordRoleRes: {Name}, guild={GuildId}, position={Position}";
}
