using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Lyo.Csv.Converters;

/// <summary>CsvHelper converter: empty or unparsable input becomes null; otherwise a <see cref="decimal"/>.</summary>
public sealed class DecimalCsvConverter : ITypeConverter
{
    /// <summary>Parses a decimal from CSV text; returns null when empty or invalid.</summary>
    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        if (decimal.TryParse(text, out var value))
            return value;

        return null;
    }

    /// <summary>Writes a decimal cell value as its string form, or empty when null.</summary>
    public string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value?.ToString() ?? "";
}