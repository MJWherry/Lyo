using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>Minimal vCard 3.0 contact text for QR encoding.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VCard3Payload : IQrPayload
{
    /// <summary>Creates a vCard with at least a formatted name.</summary>
    public VCard3Payload(string fullName, string? telephone = null, string? email = null, string? organization = null, string? url = null)
    {
        ArgumentHelpers.ThrowIfNull(fullName);
        FullName = fullName.Trim();
        Telephone = telephone?.Trim();
        Email = email?.Trim();
        Organization = organization?.Trim();
        Url = url?.Trim();
    }

    /// <summary><c>FN</c> property (required).</summary>
    public string FullName { get; }

    /// <summary>Optional <c>TEL</c>.</summary>
    public string? Telephone { get; }

    /// <summary>Optional <c>EMAIL</c>.</summary>
    public string? Email { get; }

    /// <summary>Optional <c>ORG</c>.</summary>
    public string? Organization { get; }

    /// <summary>Optional <c>URL</c> (http(s) recommended).</summary>
    public string? Url { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"VCard3Payload FN={FullName}, TEL={Telephone ?? "(none)"}, EMAIL={Email ?? "(none)"}, ORG={Organization ?? "(none)"}, URL={Url ?? "(none)"}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(FullName), "Full name (FN) cannot be empty.", nameof(FullName));

        if (!string.IsNullOrEmpty(Url)) {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out var u))
                throw new InvalidFormatException("URL must be absolute when provided.", nameof(Url), Url, "https://example.com");

            if (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps)
                throw new InvalidFormatException("URL must use http or https.", nameof(Url), Url);
        }

        var sb = new StringBuilder(128);
        sb.Append("BEGIN:VCARD\r\nVERSION:3.0\r\n");
        sb.Append("FN:").Append(QrVCardTextEscape.Escape(FullName)).Append("\r\n");

        if (!string.IsNullOrWhiteSpace(Telephone))
            sb.Append("TEL:").Append(QrVCardTextEscape.Escape(Telephone)).Append("\r\n");

        if (!string.IsNullOrWhiteSpace(Email))
            sb.Append("EMAIL:").Append(QrVCardTextEscape.Escape(Email)).Append("\r\n");

        if (!string.IsNullOrWhiteSpace(Organization))
            sb.Append("ORG:").Append(QrVCardTextEscape.Escape(Organization)).Append("\r\n");

        if (!string.IsNullOrWhiteSpace(Url))
            sb.Append("URL:").Append(QrVCardTextEscape.Escape(Url)).Append("\r\n");

        sb.Append("END:VCARD");
        return sb.ToString();
    }
}
