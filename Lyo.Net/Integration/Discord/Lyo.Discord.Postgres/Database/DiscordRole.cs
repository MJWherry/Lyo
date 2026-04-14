namespace Lyo.Discord.Postgres.Database;

public class DiscordRole
{
    public long Id { get; set; }

    public long GuildId { get; set; }

    public long? EmojiId { get; set; }

    public string Name { get; set; } = null!;

    public string? Icon { get; set; }

    public string Color { get; set; } = null!;

    public bool IsHoisted { get; set; }

    public bool IsManaged { get; set; }

    public bool IsMentionable { get; set; }

    public int Position { get; set; }

    public DateTime RoleCreatedDate { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}