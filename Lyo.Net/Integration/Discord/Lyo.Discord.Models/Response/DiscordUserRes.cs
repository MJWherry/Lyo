namespace Lyo.Discord.Models.Response;

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
    DateTime UserCreatedDate);