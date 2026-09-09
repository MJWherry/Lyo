namespace Lyo.Discord.Postgres.Database;

public class DiscordInteraction
{
    public long Id { get; set; }

    public long AuthorId { get; set; }

    public long ChannelId { get; set; }

    public long GuildId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime InteractionCreatedDate { get; set; }
}