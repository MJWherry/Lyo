using System.Text;

namespace Lyo.Book;

/// <summary>Well-known bibliographic identifier schemes and normalization.</summary>
public static class BookIdentifierSchemes
{
    public const string Isbn13 = "isbn13";
    public const string Isbn10 = "isbn10";
    public const string Lccn = "lccn";
    public const string Oclc = "oclc";
    public const string Dewey = "dewey";
    public const string LocClass = "loc-class";
    public const string OpenLibrary = "openlibrary";
    public const string Goodreads = "goodreads";
    public const string GoogleBooks = "google-books";
    public const string Asin = "asin";
    public const string Barcode = "barcode";
    public const string Custom = "custom";

    /// <summary>Returns digits-only (plus trailing X for ISBN-10) for ISBN schemes; otherwise trimmed value.</summary>
    public static string Normalize(string scheme, string value)
    {
        if (value is null)
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        if (!IsIsbn(scheme))
            return trimmed;

        var buffer = new StringBuilder(trimmed.Length);
        for (var i = 0; i < trimmed.Length; i++) {
            var c = trimmed[i];
            if (char.IsDigit(c))
                buffer.Append(c);
            else if (c is 'X' or 'x' && string.Equals(scheme, Isbn10, StringComparison.OrdinalIgnoreCase))
                buffer.Append('X');
        }

        return buffer.ToString();
    }

    public static bool IsIsbn(string? scheme)
        => string.Equals(scheme, Isbn13, StringComparison.OrdinalIgnoreCase) || string.Equals(scheme, Isbn10, StringComparison.OrdinalIgnoreCase);
}