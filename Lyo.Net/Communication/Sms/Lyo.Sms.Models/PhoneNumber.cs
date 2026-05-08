using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lyo.Sms.Models;

/// <summary>Represents a phone number and provides helpers for validation and normalization.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PhoneNumber
{
    // E.164 format: + followed by 1-15 digits (country code + number)
    // Also supports US format: (XXX) XXX-XXXX or XXX-XXX-XXXX or XXX.XXX.XXXX or 10 digits
    /// <summary>Gets the regular expression used to validate supported phone number formats.</summary>
    public static readonly Regex Regex = new(
        @"^(\+[1-9]\d{1,14})$|^(\+?\d{1,3}[\s\-\.]?)?\(?\d{3}\)?[\s\-\.]?\d{3}[\s\-\.]?\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Gets examples of accepted phone number formats.</summary>
    public static readonly string[] ValidFormats = [
        "E.164 format (e.g., +15551234567)", "US format (e.g., 555-123-4567)", "US format (e.g., (555) 123-4567)", "US format (e.g., 555.123.4567)"
    ];

    /// <summary>Gets or sets the raw or normalized phone number value.</summary>
    public string Number { get; set; } = null!;

    /// <summary>Gets or sets the country code for the phone number.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Gets or sets a display-formatted version of the phone number.</summary>
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

    /// <summary>Determines whether a phone number matches supported formats.</summary>
    /// <param name="phoneNumber">The phone number to validate.</param>
    /// <returns><see langword="true" /> when the phone number is valid; otherwise <see langword="false" />.</returns>
    public static bool IsValid(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return Regex.IsMatch(phoneNumber);
    }

    /// <summary>Returns a display-friendly representation of the phone number.</summary>
    /// <returns>The formatted number when available; otherwise the raw number.</returns>
    public override string ToString() => Formatted ?? Number;
}