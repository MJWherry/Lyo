using System.Diagnostics;

namespace Lyo.Discord.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordChannelReq
{
    public long Id { get; set; }

    public long GuildId { get; set; }

    public string? Name { get; set; }

    public string? Topic { get; set; }

    public string? ChannelType { get; set; }

    public bool IsCategory { get; set; }

    public bool IsNSFW { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsThread { get; set; }

    public int Position { get; set; }

    public long? ParentId { get; set; }

    public DateTime ChannelCreated { get; set; }

    public override string ToString()
        => $"DiscordChannelReq: id={Id}, name={Name}, guild={GuildId}, type={ChannelType}";
}
