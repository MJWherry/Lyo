using Lyo.Exceptions;
using MimeKit;

namespace Lyo.Email;

/// <summary>Parses a <c>.eml</c> message with MimeKit. There is no IMAP client here.</summary>
public static class EmailMimeParser
{
    /// <summary>Reads headers, the text and HTML bodies, and attachments.</summary>
    public static ParsedEmail Parse(byte[] eml)
    {
        ArgumentHelpers.ThrowIfNull(eml);
        using var stream = new MemoryStream(eml, writable: false);
        var message = MimeMessage.Load(stream);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in message.Headers)
            headers[header.Field] = header.Value;

        var attachments = new List<ParsedEmailAttachment>();
        foreach (var attachment in message.Attachments) {
            if (attachment is not MimePart part || part.Content is null)
                continue;
            using var content = new MemoryStream();
            part.Content.DecodeTo(content);
            attachments.Add(new(part.FileName ?? "attachment", part.ContentType.MimeType, content.ToArray()));
        }

        return new(headers, message.TextBody, message.HtmlBody, attachments);
    }
}
