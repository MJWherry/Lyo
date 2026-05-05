using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Lyo.Csv.Converters;

/// <summary>CsvHelper converter: empty or unparsable input becomes null; otherwise a 32-bit integer.</summary>
public class Int32CsvConverter : ITypeConverter
{
    /// <summary>Parses a 32-bit integer from CSV text; returns null when empty or invalid.</summary>
    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        if (int.TryParse(text, out var value))
            return value;

        return null;
    }

    /// <summary>Writes an integer cell value as its string form, or empty when null.</summary>
    public string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value?.ToString() ?? "";
}