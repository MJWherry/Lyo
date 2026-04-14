namespace Lyo.Discord.Models.Request;

public sealed class DiscordInteractionReq
{
    public long Id { get; set; }

    public long AuthorId { get; set; }

    public long ChannelId { get; set; }

    public long GuildId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime InteractionCreatedDate { get; set; }
}