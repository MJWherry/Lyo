using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lyo.Sms.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class PhoneNumber
{
    // E.164 format: + followed by 1-15 digits (country code + number)
    // Also supports US format: (XXX) XXX-XXXX or XXX-XXX-XXXX or XXX.XXX.XXXX or 10 digits
    public static readonly Regex Regex = new(
        @"^(\+[1-9]\d{1,14})$|^(\+?\d{1,3}[\s\-\.]?)?\(?\d{3}\)?[\s\-\.]?\d{3}[\s\-\.]?\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static readonly string[] ValidFormats = [
        "E.164 format (e.g., +15551234567)", "US format (e.g., 555-123-4567)", "US format (e.g., (555) 123-4567)", "US format (e.g., 555.123.4567)"
    ];

    public string Number { get; set; } = null!;

    public string? CountryCode { get; set; }

    public string? Formatted { get; set; }

    /// <summary>Normalizes a phone number to E.164 format (e.g., +1234567890)</summary>
    public static string? Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Remove all non-digit characters except +
        var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // If it doesn't start with +, assume US number and add +1
        if (!cleaned.StartsWith("+")) {
            // If it starts with 1 and has 11 digits, it's already country code + number
            if (cleaned.Length == 11 && cleaned.StartsWith("1"))
                cleaned = "+" + cleaned;
            // If it has 10 digits, assume US number
            else if (cleaned.Length == 10)
                cleaned = "+1" + cleaned;
            // Otherwise, try to add + if missing
            else if (cleaned.Length > 0)
                cleaned = "+" + cleaned;
        }

        return cleaned;
    }

    public static bool IsValid(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return Regex.IsMatch(phoneNumber);
    }

    public override string ToString() => Formatted ?? Number;
}