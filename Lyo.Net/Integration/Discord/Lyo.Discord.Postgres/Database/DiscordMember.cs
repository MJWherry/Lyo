using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lyo.Discord.Postgres.Database;

/// <summary>Membership xref between <see cref="DiscordUser" /> and <see cref="DiscordGuild" />; optional DSharp overflow in <see cref="ExtraJson" />.</summary>
public class DiscordMember
{
    public long UserId { get; set; }

    public long GuildId { get; set; }

    public DateTime? JoinedAtUtc { get; set; }

    [MaxLength(32)]
    public string? Nickname { get; set; }

    [Column(TypeName = "jsonb")]
    [MaxLength(8192)]
    public string? ExtraJson { get; set; }
}