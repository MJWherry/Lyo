using System.Diagnostics;

namespace Lyo.Email.Smtp;

/// <summary>One attachment from a parsed <c>.eml</c> message.</summary>
/// <param name="FileName">Attachment file name.</param>
/// <param name="ContentType">MIME type.</param>
/// <param name="Bytes">Decoded bytes.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ParsedEmailAttachment(string FileName, string ContentType, byte[] Bytes)
{
    /// <summary>File name, content type, and byte length.</summary>
    public override string ToString() => $"{FileName} ({ContentType}, {Bytes?.Length ?? 0} bytes)";
}

/// <summary>Headers, text body, HTML body, and attachments read from a <c>.eml</c> message. This does not talk to a mailbox.</summary>
/// <param name="Headers">Header name to value. Later duplicates replace earlier ones.</param>
/// <param name="TextBody">Plain-text body, when present.</param>
/// <param name="HtmlBody">HTML body, when present.</param>
/// <param name="Attachments">Decoded attachments.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record ParsedEmail(IReadOnlyDictionary<string, string> Headers, string? TextBody, string? HtmlBody, IReadOnlyList<ParsedEmailAttachment> Attachments)
{
    /// <summary>Subject, counts, and body lengths. Body text is omitted.</summary>
    public override string ToString()
    {
        Headers.TryGetValue("Subject", out var subject);
        return $"Subject={subject} Headers={Headers.Count} Text={TextBody?.Length ?? 0} Html={HtmlBody?.Length ?? 0} Attachments={Attachments.Count}";
    }
}
