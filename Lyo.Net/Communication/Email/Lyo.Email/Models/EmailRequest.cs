using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>An email: sender, recipients, subject, bodies, and optional headers.</summary>
/// <param name="FromAddress">Sender address.</param>
/// <param name="FromName">Sender display name.</param>
/// <param name="ToAddresses">Primary recipients.</param>
/// <param name="CcAddresses">CC recipients.</param>
/// <param name="BccAddresses">BCC recipients.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="Attachments">Optional attachments. <see cref="EmailAttachment.MetadataJson" /> is not sent on the wire.</param>
/// <param name="TextBody">Plain-text body, when present.</param>
/// <param name="HtmlBody">HTML body, when present. Hosts that log to Postgres write this to their own file; it is not a FileStorage id.</param>
/// <param name="ReplyTo">Reply-To recipients. <see cref="EmailRequestBuilder" /> appends each call.</param>
/// <param name="Headers">Custom headers.</param>
/// <param name="Priority">Optional priority. Null leaves the provider default.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailRequest(
    string? FromAddress = null,
    string? FromName = null,
    IReadOnlyList<EmailAddress>? ToAddresses = null,
    IReadOnlyList<EmailAddress>? CcAddresses = null,
    IReadOnlyList<EmailAddress>? BccAddresses = null,
    string? Subject = null,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    string? TextBody = null,
    string? HtmlBody = null,
    IReadOnlyList<EmailAddress>? ReplyTo = null,
    IReadOnlyList<EmailHeader>? Headers = null,
    EmailPriority? Priority = null)
{
    /// <summary>Readable summary of sender, recipients, and subject.</summary>
    /// <returns>Human-readable form of this request.</returns>
    public override string ToString()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(Subject))
            parts.Add($"Subject: {Subject}");

        if (!string.IsNullOrWhiteSpace(FromAddress))
            parts.Add($"From: {FromAddress}");

        if (ToAddresses?.Count > 0)
            parts.Add($"To: {string.Join(", ", ToAddresses)}");

        if (CcAddresses?.Count > 0)
            parts.Add($"Cc: {string.Join(", ", CcAddresses)}");

        if (BccAddresses?.Count > 0)
            parts.Add($"Bcc: {string.Join(", ", BccAddresses)}");

        return string.Join(" | ", parts);
    }
}
