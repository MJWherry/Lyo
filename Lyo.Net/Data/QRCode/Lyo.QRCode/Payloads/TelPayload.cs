using System.Diagnostics;
using System.Text;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary><c>tel:</c> URI (international format recommended).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TelPayload : IQrPayload
{
    /// <summary>Creates a <c>tel:</c> payload from a phone string (spaces and common separators are stripped except leading <c>+</c>).</summary>
    public TelPayload(string phoneNumber)
    {
        ArgumentHelpers.ThrowIfNull(phoneNumber);
        PhoneNumber = phoneNumber.Trim();
    }

    /// <summary>Raw input (trimmed).</summary>
    public string PhoneNumber { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"TelPayload {PhoneNumber}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(PhoneNumber), "Phone number cannot be empty.", nameof(PhoneNumber));

        var normalized = NormalizePhone(PhoneNumber);
        if (normalized.Length == 0)
            throw new InvalidFormatException("Phone number has no digits.", nameof(PhoneNumber), PhoneNumber, "+15551234567", "5551234567");

        return "tel:" + normalized;
    }

    /// <summary>Strips visual separators; preserves leading <c>+</c> for E.164.</summary>
    public static string NormalizePhone(string phoneNumber)
    {
        var sb = new StringBuilder(phoneNumber.Length);
        var seenPlus = false;
        foreach (var c in phoneNumber) {
            if (c == '+' && !seenPlus && sb.Length == 0) {
                sb.Append(c);
                seenPlus = true;
            }
            else if (char.IsAsciiDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }
}
