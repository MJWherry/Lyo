using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordMemberRes(long UserId, long GuildId, DateTime? JoinedAtUtc, string? Nickname, string? ExtraJson)
{
    public override string ToString()
        => $"DiscordMemberRes: user={UserId}, guild={GuildId}, nick={Nickname}";
}
