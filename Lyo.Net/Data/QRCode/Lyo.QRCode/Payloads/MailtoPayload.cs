using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary><c>mailto:</c> URI with optional headers.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MailtoPayload : IQrPayload
{
    /// <summary>Initializes a mailto payload.</summary>
    /// <param name="to">Primary recipient (one address; no <c>mailto:</c> prefix).</param>
    /// <param name="subject">Optional subject.</param>
    /// <param name="body">Optional body.</param>
    /// <param name="cc">Optional CC addresses (comma-separated).</param>
    /// <param name="bcc">Optional BCC addresses (comma-separated).</param>
    public MailtoPayload(string to, string? subject = null, string? body = null, string? cc = null, string? bcc = null)
    {
        ArgumentHelpers.ThrowIfNull(to);
        To = to.Trim();
        Subject = subject?.Trim();
        Body = body;
        Cc = cc?.Trim();
        Bcc = bcc?.Trim();
    }

    /// <summary>To address.</summary>
    public string To { get; }

    /// <summary>Optional subject.</summary>
    public string? Subject { get; }

    /// <summary>Optional body.</summary>
    public string? Body { get; }

    /// <summary>Optional CC list.</summary>
    public string? Cc { get; }

    /// <summary>Optional BCC list.</summary>
    public string? Bcc { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"MailtoPayload to={To}, subjectLen={Subject?.Length ?? 0}, bodyLen={Body?.Length ?? 0}, cc={(Cc != null)}, bcc={(Bcc != null)}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(To), "To address cannot be empty.", nameof(To));

        if (To.Contains('\r') || To.Contains('\n'))
            throw new InvalidFormatException("To address cannot contain newlines.", nameof(To), To, "user@example.com");

        var sb = new StringBuilder("mailto:");
        sb.Append(Uri.EscapeDataString(To));

        var q = new List<string>(4);
        if (!string.IsNullOrEmpty(Subject))
            q.Add("subject=" + Uri.EscapeDataString(Subject));

        if (!string.IsNullOrEmpty(Body))
            q.Add("body=" + Uri.EscapeDataString(Body));

        if (!string.IsNullOrEmpty(Cc))
            q.Add("cc=" + Uri.EscapeDataString(Cc));

        if (!string.IsNullOrEmpty(Bcc))
            q.Add("bcc=" + Uri.EscapeDataString(Bcc));

        if (q.Count > 0)
            sb.Append('?').Append(string.Join('&', q));

        var s = sb.ToString();
        if (!Uri.TryCreate(s, UriKind.Absolute, out _))
            throw new InvalidFormatException("mailto URI could not be built.", nameof(To), s, "mailto:user@example.com");

        return s;
    }
}
