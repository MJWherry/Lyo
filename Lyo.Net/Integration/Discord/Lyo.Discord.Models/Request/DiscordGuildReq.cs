namespace Lyo.Discord.Models.Request;

public sealed class DiscordGuildReq
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
}