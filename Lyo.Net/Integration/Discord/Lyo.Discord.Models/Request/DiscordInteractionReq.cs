using System.Diagnostics;

namespace Lyo.Discord.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordInteractionReq
{
    public long Id { get; set; }

    public long AuthorId { get; set; }

    public long ChannelId { get; set; }

    public long GuildId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime InteractionCreatedDate { get; set; }

    public override string ToString() => $"DiscordInteractionReq: id={Id}, author={AuthorId}, guild={GuildId}";
}