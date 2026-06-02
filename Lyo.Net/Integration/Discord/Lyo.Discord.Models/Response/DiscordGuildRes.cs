using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordGuildRes(
    long Id,
    long OwnerId,
    string Name,
    string? Description,
    int MemberCount,
    int CurrentSubscriptionCount,
    bool IsLarge,
    bool IsNSFW,
    bool IsUnavailable,
    DateTime GuildCreatedDate,
    DateTime JoinedDate)
{
    public override string ToString()
        => $"DiscordGuildRes: id={Id}, name={Name}, members={MemberCount}";
}
