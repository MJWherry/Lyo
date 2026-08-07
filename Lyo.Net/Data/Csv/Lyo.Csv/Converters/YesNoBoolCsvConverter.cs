using System.Globalization;
using Lyo.Csv.Models;

namespace Lyo.Csv.Converters;

/// <summary>Booleans as <c>yes</c>/<c>no</c> (case-insensitive); also maps 0/1 when writing.</summary>
public sealed class YesNoBoolCsvConverter : ICsvValueConverter
{
    /// <inheritdoc />
    public object? ConvertFromString(string? text, CultureInfo culture)
    {
        if (text is null || string.IsNullOrEmpty(text))
            return null;

        if (text.Equals("yes", StringComparison.OrdinalIgnoreCase) || text == "1")
            return true;

        if (text.Equals("no", StringComparison.OrdinalIgnoreCase) || text == "0")
            return false;

        if (bool.TryParse(text, out var b))
            return b;

        return null;
    }

    /// <inheritdoc />
    public string ConvertToString(object? value, CultureInfo culture)
        => value switch {
            bool b => b ? "yes" : "no",
            int i => i switch {
                0 => "no",
                1 => "yes",
                _ => ""
            },
            _ => ""
        };
}
