using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary><c>sms:</c> or <c>smsto:</c> URI. Defaults to <c>sms:</c> (RFC-style); <c>smsto:</c> can crash Google Messages on Android when combined with a long <c>body</c> query.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SmsPayload : IQrPayload
{
    /// <summary>Many OEM SMS apps mishandle very long URIs; keep QR strings below this length.</summary>
    public const int MaxSmsQrUriLength = 1800;

    /// <summary>Creates an SMS QR payload.</summary>
    /// <param name="phoneNumber">Destination number (normalized like <see cref="TelPayload.NormalizePhone" />).</param>
    /// <param name="body">Optional message body.</param>
    /// <param name="useSmstoScheme">If true, uses <c>smsto:</c>; otherwise <c>sms:</c> (recommended for Android).</param>
    public SmsPayload(string phoneNumber, string? body = null, bool useSmstoScheme = false)
    {
        ArgumentHelpers.ThrowIfNull(phoneNumber);
        PhoneNumber = phoneNumber.Trim();
        Body = body;
        UseSmstoScheme = useSmstoScheme;
    }

    /// <summary>Destination phone.</summary>
    public string PhoneNumber { get; }

    /// <summary>Optional SMS body.</summary>
    public string? Body { get; }

    /// <summary>Scheme choice.</summary>
    public bool UseSmstoScheme { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"{(UseSmstoScheme ? "smsto" : "sms")}:{PhoneNumber}, bodyLen={Body?.Length ?? 0}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(PhoneNumber), "Phone number cannot be empty.", nameof(PhoneNumber));

        var normalized = TelPayload.NormalizePhone(PhoneNumber);
        if (normalized.Length == 0)
            throw new InvalidFormatException("Phone number has no digits.", nameof(PhoneNumber), PhoneNumber, "+15551234567");

        var scheme = UseSmstoScheme ? "smsto:" : "sms:";
        var sb = new StringBuilder(scheme.Length + normalized.Length + 16);
        // smsto:/sms: recipients are digits with optional leading +; do not percent-encode + (many clients expect literal +).
        sb.Append(scheme).Append(normalized);

        if (!string.IsNullOrEmpty(Body))
            sb.Append('?').Append("body=").Append(Uri.EscapeDataString(Body));

        var s = sb.ToString();
        if (s.Length > MaxSmsQrUriLength)
            throw new InvalidFormatException(
                $"SMS QR URI is {s.Length} characters; many Android SMS apps crash or fail above ~{MaxSmsQrUriLength}. Shorten the message body or send a shorter link.",
                nameof(Body),
                Body,
                $"body under ~{MaxSmsQrUriLength - 200} characters for typical phone numbers");

        return s;
    }
}
