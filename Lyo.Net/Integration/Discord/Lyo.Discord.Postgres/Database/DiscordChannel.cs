namespace Lyo.Discord.Postgres.Database;

public class DiscordChannel
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

    public DateTime CreatedTimestamp { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}