namespace Lyo.Email;

/// <summary>One attachment from a parsed <c>.eml</c> message.</summary>
public sealed record ParsedEmailAttachment(string FileName, string ContentType, byte[] Bytes);

/// <summary>Headers, text body, HTML body, and attachments read from a <c>.eml</c> message. This does not talk to a mailbox.</summary>
public sealed record ParsedEmail(IReadOnlyDictionary<string, string> Headers, string? TextBody, string? HtmlBody, IReadOnlyList<ParsedEmailAttachment> Attachments);