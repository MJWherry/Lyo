namespace Lyo.Discord.Models.Response;

public sealed record DiscordAttachmentRes(
    long Id,
    long? InteractionId,
    long? MessageId,
    string Filename,
    int FileSize,
    string MediaType,
    string ProxyUrl,
    string Url,
    DateTime AttachmentCreatedDate);