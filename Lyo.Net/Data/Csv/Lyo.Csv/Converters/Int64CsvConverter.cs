using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Lyo.Csv.Converters;

public sealed class Int64CsvConverter : ITypeConverter
{
    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        if (long.TryParse(text, out var value))
            return value;

        return null;
    }

    public string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value?.ToString() ?? "";
}