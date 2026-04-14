namespace Lyo.Discord.Models.Response;

public sealed record DiscordMemberRes(long UserId, long GuildId, DateTime? JoinedAtUtc, string? Nickname, string? ExtraJson);