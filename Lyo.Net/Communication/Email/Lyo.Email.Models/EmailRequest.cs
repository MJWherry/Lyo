using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>Represents an email message with sender, recipients, and subject.</summary>
/// <param name="FromAddress">The sender email address.</param>
/// <param name="FromName">The sender display name.</param>
/// <param name="ToAddresses">Primary recipient email addresses.</param>
/// <param name="CcAddresses">Carbon-copy recipient email addresses.</param>
/// <param name="BccAddresses">Blind-carbon-copy recipient email addresses.</param>
/// <param name="Subject">The email subject line.</param>
/// <param name="Attachments">Optional attachment metadata associated with the email.</param>
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
    /// <summary>Returns a readable summary of sender, recipients, and subject.</summary>
    /// <returns>A human-readable string representing this email request.</returns>
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