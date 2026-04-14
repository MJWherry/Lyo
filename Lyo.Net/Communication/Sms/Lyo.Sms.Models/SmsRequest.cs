using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>Represents an SMS message with recipient, sender, body, and optional media attachments.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SmsRequest
{
    /// <summary>Gets or sets the recipient phone number (E.164 format).</summary>
    public string? To { get; set; }

    /// <summary>Gets or sets the sender phone number (E.164 format).</summary>
    public string? From { get; set; }

    /// <summary>Gets or sets the message body text.</summary>
    public string? Body { get; set; }

    /// <summary>Gets or sets the list of media URLs for MMS attachments.</summary>
    public List<Uri> MediaUrls { get; init; } = [];

    public SmsRequest() { }

    public SmsRequest(string to, string? body = null, string? from = null)
    {
        To = to;
        Body = body;
        From = from;
    }

    public override string ToString()
    {
        var parts = new List<string> { $"To: {To}", $"From: {From}", $"Body: {Body?.Substring(0, Math.Min(Body?.Length ?? 0, 50))}{(Body?.Length > 50 ? "..." : "")}" };
        if (MediaUrls.Count > 0)
            parts.Add($"Media: {MediaUrls.Count} attachment(s)");

        return string.Join(" | ", parts);
    }
}