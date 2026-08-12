using System.Globalization;
using Lyo.Csv.Models;

namespace Lyo.Csv.Converters;

/// <summary>Empty or unparsable input becomes null; otherwise a <see cref="long" />.</summary>
public sealed class Int64CsvConverter : ICsvValueConverter
{
    /// <inheritdoc />
    public object? ConvertFromString(string? text, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        return long.TryParse(text, NumberStyles.Integer, culture, out var value) ? value : null;
    }

    /// <inheritdoc />
    public string ConvertToString(object? value, CultureInfo culture) => value is IFormattable f ? f.ToString(null, culture) ?? "" : value?.ToString() ?? "";
}