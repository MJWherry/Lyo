namespace Lyo.Discord.Models.Response;

public sealed record DiscordInteractionRes(long Id, long AuthorId, long ChannelId, long GuildId, string Content, DateTime InteractionCreatedDate);