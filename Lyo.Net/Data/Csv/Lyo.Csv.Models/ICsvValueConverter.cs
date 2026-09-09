using System.Globalization;

namespace Lyo.Csv.Models;

/// <summary>Converts between CSV cell text and CLR values for typed mapping.</summary>
public interface ICsvValueConverter
{
    /// <summary>Coerces <paramref name="text" /> to a CLR value under <paramref name="culture" />. Empty or invalid input may yield null.</summary>
    object? ConvertFromString(string? text, CultureInfo culture);

    /// <summary>Renders <paramref name="value" /> as cell text under <paramref name="culture" />.</summary>
    string ConvertToString(object? value, CultureInfo culture);
}