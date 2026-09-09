using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Lyo.Sms.Models;

/// <summary>A phone number plus helpers to validate and normalize it.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class PhoneNumber
{
    // E.164: + then 1-15 digits (country code plus national number)
    // US forms also accepted: (XXX) XXX-XXXX, XXX-XXX-XXXX, XXX.XXX.XXXX, or 10 digits
    /// <summary>Regex that accepts the supported phone-number forms.</summary>
    public static readonly Regex Regex = new(
        @"^(\+[1-9]\d{1,14})$|^(\+?\d{1,3}[\s\-\.]?)?\(?\d{3}\)?[\s\-\.]?\d{3}[\s\-\.]?\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Example strings that illustrate accepted formats.</summary>
    public static readonly string[] ValidFormats = [
        "E.164 format (e.g., +15551234567)", "US format (e.g., 555-123-4567)", "US format (e.g., (555) 123-4567)", "US format (e.g., 555.123.4567)"
    ];

    /// <summary>Raw or normalized number string.</summary>
    public string Number { get; set; } = null!;

    /// <summary>Country code portion.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Display-formatted number, when available.</summary>
    public string? Formatted { get; set; }

    /// <summary>Rewrites a number into E.164 (for example +1234567890).</summary>
    public static string? Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        // Strip everything except digits and +
        var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // Missing + is treated as a US number and prefixed with +1
        if (cleaned.StartsWith("+"))
            return cleaned;

        // 11 digits starting with 1 already include the country code
        if (cleaned.Length == 11 && cleaned.StartsWith("1"))
            cleaned = "+" + cleaned;
        // 10 digits are treated as a US national number
        else if (cleaned.Length == 10)
            cleaned = "+1" + cleaned;
        // Otherwise prefix + when it is absent
        else if (cleaned.Length > 0)
            cleaned = "+" + cleaned;

        return cleaned;
    }

    /// <summary>Whether <paramref name="phoneNumber" /> matches a supported format.</summary>
    /// <param name="phoneNumber">Number to check.</param>
    /// <returns><see langword="true" /> if valid; otherwise <see langword="false" />.</returns>
    public static bool IsValid(string phoneNumber) => !string.IsNullOrWhiteSpace(phoneNumber) && Regex.IsMatch(phoneNumber);

    /// <summary>Display form of the number.</summary>
    /// <returns>Formatted text when set; otherwise the raw number.</returns>
    public override string ToString() => Formatted ?? Number;
}