using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordChannelRes(
    long Id,
    long GuildId,
    string? Name,
    string? Topic,
    string? ChannelType,
    bool IsCategory,
    bool IsNSFW,
    bool IsPrivate,
    bool IsThread,
    int Position,
    long? ParentId,
    DateTime ChannelCreated)
{
    public override string ToString()
        => $"DiscordChannelRes: #{Name}, guild={GuildId}, type={ChannelType}";
}
