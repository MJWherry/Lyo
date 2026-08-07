using System.Globalization;
using Lyo.Csv.Models;

namespace Lyo.Csv.Converters;

/// <summary>Empty or unparsable input becomes null; otherwise an <see cref="int" />.</summary>
public class Int32CsvConverter : ICsvValueConverter
{
    /// <inheritdoc />
    public object? ConvertFromString(string? text, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        return int.TryParse(text, NumberStyles.Integer, culture, out var value) ? value : null;
    }

    /// <inheritdoc />
    public string ConvertToString(object? value, CultureInfo culture)
        => value is IFormattable f ? f.ToString(null, culture) ?? "" : value?.ToString() ?? "";
}
