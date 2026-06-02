using System.Diagnostics;

namespace Lyo.Discord.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordMemberReq
{
    public long UserId { get; set; }

    public long GuildId { get; set; }

    public DateTime? JoinedAtUtc { get; set; }

    public string? Nickname { get; set; }

    public string? ExtraJson { get; set; }

    public override string ToString()
        => $"DiscordMemberReq: user={UserId}, guild={GuildId}, nickname={Nickname}";
}
