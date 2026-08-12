using System.Globalization;

namespace Lyo.Csv.Models;

/// <summary>Converts between CSV cell text and CLR values for typed mapping.</summary>
public interface ICsvValueConverter
{
    /// <summary>Parses <paramref name="text" /> to a CLR value using <paramref name="culture" />. May return null for empty/invalid input.</summary>
    object? ConvertFromString(string? text, CultureInfo culture);

    /// <summary>Formats <paramref name="value" /> as CSV cell text using <paramref name="culture" />.</summary>
    string ConvertToString(object? value, CultureInfo culture);
}