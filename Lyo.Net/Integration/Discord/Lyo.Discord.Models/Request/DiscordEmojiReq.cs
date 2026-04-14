namespace Lyo.Discord.Models.Request;

public sealed class DiscordEmojiReq
{
    public long Id { get; set; }

    public long? GuildId { get; set; }

    public string Name { get; set; } = null!;

    public string? Url { get; set; }

    public bool IsAnimated { get; set; }

    public bool IsAvailable { get; set; }

    public bool IsManaged { get; set; }

    public bool RequiresColons { get; set; }

    public DateTime EmojiCreatedDate { get; set; }
}