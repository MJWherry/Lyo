namespace Lyo.Discord.Postgres.Database;

public class DiscordAttachment
{
    public long Id { get; set; }

    public long? InteractionId { get; set; }

    public long? MessageId { get; set; }

    public string Filename { get; set; } = null!;

    public int FileSize { get; set; }

    public string MediaType { get; set; } = null!;

    public string ProxyUrl { get; set; } = null!;

    public string Url { get; set; } = null!;

    public DateTime AttachmentCreatedDate { get; set; }
}