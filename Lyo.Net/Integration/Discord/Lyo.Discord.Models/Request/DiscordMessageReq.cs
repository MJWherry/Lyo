using System.Diagnostics;

namespace Lyo.Discord.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordMessageReq
{
    public long Id { get; set; }

    public long AuthorId { get; set; }

    public long ChannelId { get; set; }

    public long GuildId { get; set; }

    public string? Content { get; set; }

    public bool IsEdited { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime MessageCreatedDate { get; set; }

    public override string ToString() => $"DiscordMessageReq: id={Id}, channel={ChannelId}, author={AuthorId}";
}