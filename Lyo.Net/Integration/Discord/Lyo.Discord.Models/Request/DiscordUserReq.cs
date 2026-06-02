using System.Diagnostics;

namespace Lyo.Discord.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordUserReq
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public int Discriminator { get; set; }

    public string? Email { get; set; }

    public string? Locale { get; set; }

    public bool? IsVerified { get; set; }

    public bool IsBot { get; set; }

    public bool? IsSystem { get; set; }

    public bool? IsMfaEnabled { get; set; }

    public string? PremiumLevel { get; set; }

    public DateTime UserCreatedDate { get; set; }

    public override string ToString()
        => $"DiscordUserReq: id={Id}, username={Username}, bot={IsBot}";
}
