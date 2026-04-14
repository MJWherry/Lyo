namespace Lyo.Discord.Models.Request;

public sealed class DiscordMemberReq
{
    public long UserId { get; set; }

    public long GuildId { get; set; }

    public DateTime? JoinedAtUtc { get; set; }

    public string? Nickname { get; set; }

    public string? ExtraJson { get; set; }
}