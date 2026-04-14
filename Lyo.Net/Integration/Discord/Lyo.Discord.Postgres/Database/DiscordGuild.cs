namespace Lyo.Discord.Postgres.Database;

public class DiscordGuild
{
    public long Id { get; set; }

    public long OwnerId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int MemberCount { get; set; }

    public int CurrentSubscriptionCount { get; set; }

    public bool IsLarge { get; set; }

    public bool IsNSFW { get; set; }

    public bool IsUnavailable { get; set; }

    public DateTime GuildCreatedDate { get; set; }

    public DateTime JoinedDate { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime UpdatedTimestamp { get; set; }
}