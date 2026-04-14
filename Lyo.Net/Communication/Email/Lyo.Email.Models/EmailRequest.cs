using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>Represents an email message with sender, recipients, and subject.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailRequest(
    string? FromAddress = null,
    string? FromName = null,
    IReadOnlyList<string>? ToAddresses = null,
    IReadOnlyList<string>? CcAddresses = null,
    IReadOnlyList<string>? BccAddresses = null,
    string? Subject = null,
    IReadOnlyList<EmailAttachment>? Attachments = null)
{
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