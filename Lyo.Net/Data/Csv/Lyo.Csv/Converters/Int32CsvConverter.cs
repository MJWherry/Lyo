using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Lyo.Csv.Converters;

public class Int32CsvConverter : ITypeConverter
{
    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        if (int.TryParse(text, out var value))
            return value;

        return null;
    }

    public string ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value?.ToString() ?? "";
}