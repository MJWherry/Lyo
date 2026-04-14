using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Lyo.Csv.Converters;

public sealed class YesNoBoolCsvConverter : ITypeConverter
{
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