using System.Text.RegularExpressions;

namespace Lyo.Common;

public static class RegexPatterns
{
    public static readonly Regex PhoneNumberRegex = new(@"(?:\+(\d{0,3}))?[\D{1}]?(\d{3})[\D{1}]?[\W]?(\d{3})[\W]?(\d{4})");

    public static readonly Regex SsnRegex = new(@"(\d{3})-(\d{2})-(\d{4})");

    public static readonly Regex SensitiveUriRegex = new(@"(?i:(?<!result|status)(\w*secret|\w*token|\bcode\b|\w*password))(?:\s|=|:)(.*?)(?:\s|&)");

    /// <summary>Basic email validation pattern (RFC 5322 simplified).</summary>
    public static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$", RegexOptions.Compiled);

    /// <summary>US ZIP code: 12345 or 12345-6789.</summary>
    public static readonly Regex UsZipCodeRegex = new(@"^\d{5}(?:-\d{4})?$", RegexOptions.Compiled);

    /// <summary>Matches credit card numbers for masking (4 groups of 4 digits).</summary>
    public static readonly Regex CreditCardMaskRegex = new(@"\b(?:\d{4}[\s-]?){3}\d{4}\b", RegexOptions.Compiled);
}