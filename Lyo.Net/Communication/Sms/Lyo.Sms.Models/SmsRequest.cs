using System.Diagnostics;

namespace Lyo.Sms.Models;

/// <summary>One SMS: recipient, sender, body, and optional media.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class SmsRequest
{
    /// <summary>Recipient number in E.164 form.</summary>
    public string? To { get; set; }

    /// <summary>Sender number in E.164 form.</summary>
    public string? From { get; set; }

    /// <summary>Message body.</summary>
    public string? Body { get; set; }

    /// <summary>Media URLs for MMS attachments.</summary>
    public List<Uri> MediaUrls { get; init; } = [];

    /// <summary>Empty request.</summary>
    public SmsRequest() { }

    /// <summary>Builds a request with a recipient and optional body and sender.</summary>
    /// <param name="to">Recipient number.</param>
    /// <param name="body">Body text.</param>
    /// <param name="from">Sender number.</param>
    public SmsRequest(string to, string? body = null, string? from = null)
    {
        To = to;
        Body = body;
        From = from;
    }

    /// <summary>Readable summary of this request.</summary>
    /// <returns>Text covering recipient, sender, a truncated body, and media count.</returns>
    public override string ToString()
    {
        var parts = new List<string> { $"To: {To}", $"From: {From}", $"Body: {Body?.Substring(0, Math.Min(Body?.Length ?? 0, 50))}{(Body?.Length > 50 ? "..." : "")}" };
        if (MediaUrls.Count > 0)
            parts.Add($"Media: {MediaUrls.Count} attachment(s)");

        return string.Join(" | ", parts);
    }
}