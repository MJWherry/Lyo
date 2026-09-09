namespace Lyo.Discord.Postgres.Database;

public class DiscordEmoji
{
    public long Id { get; set; }

    public long? GuildId { get; set; }

    public string Name { get; set; } = null!;

    public string? Url { get; set; }

    public bool IsAnimated { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsManaged { get; set; }

    public bool RequiresColons { get; set; }

    public DateTime EmojiCreatedDate { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}