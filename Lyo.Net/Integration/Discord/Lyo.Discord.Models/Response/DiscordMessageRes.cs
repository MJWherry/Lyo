namespace Lyo.Discord.Models.Response;

public sealed record DiscordMessageRes(long Id, long AuthorId, long ChannelId, long GuildId, string? Content, bool IsEdited, bool IsDeleted, DateTime MessageCreatedDate);