namespace Lyo.Discord.Models.Response;

public sealed record DiscordChannelRes(
    long Id,
    long GuildId,
    string? Name,
    string? Topic,
    string? ChannelType,
    bool IsCategory,
    bool IsNSFW,
    bool IsPrivate,
    bool IsThread,
    int Position,
    long? ParentId,
    DateTime ChannelCreated);