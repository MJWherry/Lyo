using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordUserRes(
    long Id,
    string Username,
    int Discriminator,
    string? Email,
    string? Locale,
    bool? IsVerified,
    bool IsBot,
    bool? IsSystem,
    bool? IsMfaEnabled,
    string? PremiumLevel,
    DateTime UserCreatedDate)
{
    public override string ToString() => $"DiscordUserRes: id={Id}, username={Username}, bot={IsBot}";
}