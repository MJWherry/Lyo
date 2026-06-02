using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordMessageRes(long Id, long AuthorId, long ChannelId, long GuildId, string? Content, bool IsEdited, bool IsDeleted, DateTime MessageCreatedDate)
{
    public override string ToString()
        => $"DiscordMessageRes: id={Id}, channel={ChannelId}, edited={IsEdited}, deleted={IsDeleted}";
}
