namespace Lyo.Discord.Models.Response;

public sealed record DiscordGuildRes(
    long Id,
    long OwnerId,
    string Name,
    string? Description,
    int MemberCount,
    int CurrentSubscriptionCount,
    bool IsLarge,
    bool IsNSFW,
    bool IsUnavailable,
    DateTime GuildCreatedDate,
    DateTime JoinedDate);