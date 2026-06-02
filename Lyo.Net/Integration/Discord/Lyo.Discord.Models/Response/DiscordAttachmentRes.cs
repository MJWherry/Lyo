using System.Diagnostics;

namespace Lyo.Discord.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record DiscordAttachmentRes(
    long Id,
    long? InteractionId,
    long? MessageId,
    string Filename,
    int FileSize,
    string MediaType,
    string ProxyUrl,
    string Url,
    DateTime AttachmentCreatedDate)
{
    public override string ToString()
        => $"DiscordAttachmentRes: {Filename}, {FileSize} bytes, {MediaType}";
}
