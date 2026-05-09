using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>Signal URI scheme for adding a contact by phone (<c>sgnl://signal.me/#p/&lt;E164&gt;</c>). Client support varies by platform.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SignalUrlPayload : IQrPayload
{
    /// <summary>Creates a Signal link from a phone number (normalized to E.164 with leading <c>+</c>).</summary>
    public SignalUrlPayload(string phoneNumber)
    {
        ArgumentHelpers.ThrowIfNull(phoneNumber);
        PhoneNumber = phoneNumber.Trim();
    }

    /// <summary>Input phone (trimmed).</summary>
    public string PhoneNumber { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"SignalUrlPayload {PhoneNumber}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(PhoneNumber), "Phone number cannot be empty.", nameof(PhoneNumber));

        var normalized = TelPayload.NormalizePhone(PhoneNumber);
        if (normalized.Length == 0)
            throw new InvalidFormatException("Phone number has no digits.", nameof(PhoneNumber), PhoneNumber, "+15551234567");

        if (!normalized.StartsWith("+", StringComparison.Ordinal))
            throw new InvalidFormatException("Signal URI expects E.164 with a leading +.", nameof(PhoneNumber), PhoneNumber, "+15551234567");

        // Fragment uses E.164 with literal + (digits and plus only).
        return "sgnl://signal.me/#p/" + normalized;
    }
}
