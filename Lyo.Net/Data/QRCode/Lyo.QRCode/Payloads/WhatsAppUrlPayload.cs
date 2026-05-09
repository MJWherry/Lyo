using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>Opens a WhatsApp chat: <c>https://wa.me/&lt;E164 digits&gt;</c> (no <c>+</c> in the path).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhatsAppUrlPayload : IQrPayload
{
    /// <summary>Creates a WhatsApp deep link from a phone string (digits and optional leading <c>+</c>).</summary>
    public WhatsAppUrlPayload(string phoneNumber)
    {
        ArgumentHelpers.ThrowIfNull(phoneNumber);
        PhoneNumber = phoneNumber.Trim();
    }

    /// <summary>Input phone (trimmed).</summary>
    public string PhoneNumber { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"WhatsAppUrlPayload {PhoneNumber}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(PhoneNumber), "Phone number cannot be empty.", nameof(PhoneNumber));

        var digits = new StringBuilder(PhoneNumber.Length);
        foreach (var c in PhoneNumber) {
            if (char.IsAsciiDigit(c))
                digits.Append(c);
        }

        if (digits.Length == 0)
            throw new InvalidFormatException("WhatsApp link requires at least one digit.", nameof(PhoneNumber), PhoneNumber, "15551234567", "+15551234567");

        return "https://wa.me/" + digits;
    }
}
