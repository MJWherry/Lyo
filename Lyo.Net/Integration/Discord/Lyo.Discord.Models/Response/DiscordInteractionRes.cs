using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordInteractionRes(long Id, long AuthorId, long ChannelId, long GuildId, string Content, DateTime InteractionCreatedDate)
{
    public override string ToString()
        => $"DiscordInteractionRes: id={Id}, guild={GuildId}, author={AuthorId}";
}
