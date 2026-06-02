using System.Diagnostics;

namespace Lyo.Discord.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class DiscordAttachmentReq
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

    public override string ToString()
        => $"DiscordAttachmentReq: id={Id}, filename={Filename}, size={FileSize}";
}
