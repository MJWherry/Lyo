using System.Text;

namespace Lyo.Audiobook;

/// <summary>Well-known audiobook identifier schemes and normalization.</summary>
public static class AudiobookIdentifierSchemes
{
    public const string Isbn = "isbn";

    public const string Asin = "asin";

    public const string Custom = "custom";

    /// <summary>
    /// Trims the value. <see cref="Isbn" /> keeps digits and a trailing X. <see cref="Asin" /> and <see cref="Custom" /> stay trim-only, so a different case does not match.
    /// </summary>
    public static string Normalize(string scheme, string value)
    {
        if (value is null)
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        if (!string.Equals(scheme, Isbn, StringComparison.OrdinalIgnoreCase))
            return trimmed;

        var buffer = new StringBuilder(trimmed.Length);
        for (var i = 0; i < trimmed.Length; i++) {
            var c = trimmed[i];
            if (char.IsDigit(c))
                buffer.Append(c);
        }

        if (LastSignificant(trimmed) is 'X' or 'x')
            buffer.Append('X');

        return buffer.ToString();
    }

    private static char LastSignificant(string trimmed)
    {
        for (var i = trimmed.Length - 1; i >= 0; i--) {
            var c = trimmed[i];
            if (c is '-' or ' ')
                continue;
            return c;
        }

        return '\0';
    }
}
