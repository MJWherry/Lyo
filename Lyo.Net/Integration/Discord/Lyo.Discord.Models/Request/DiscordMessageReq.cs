namespace Lyo.Discord.Models.Request;

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
}