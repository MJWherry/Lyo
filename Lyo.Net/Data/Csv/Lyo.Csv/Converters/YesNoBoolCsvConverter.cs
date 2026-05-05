using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Lyo.Csv.Converters;

/// <summary>CsvHelper converter for booleans as <c>yes</c>/<c>no</c> (case-insensitive); also maps 0/1 when writing.</summary>
public sealed class YesNoBoolCsvConverter : ITypeConverter
{
    /// <summary>Reads booleans as yes/no (or returns null when not recognized).</summary>
    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (text is null || string.IsNullOrEmpty(text))
            return null;

        if (text.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;

        if (text.Equals("no", StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    /// <summary>Writes booleans as yes/no and 0/1; returns null for other values.</summary>
    public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        => value switch {
            bool b => b ? "yes" : "no",
            int i => i switch {
                0 => "no",
                1 => "yes",
                var _ => null
            },
            var _ => null
        };
}