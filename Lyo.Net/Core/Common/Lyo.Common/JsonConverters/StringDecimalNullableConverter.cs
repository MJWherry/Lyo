using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

/// <summary>Deserializes JSON numbers or strings into a nullable <see cref="decimal" />; empty or unparseable strings become null.</summary>
public class StringDecimalNullableConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.Null => null,
            JsonTokenType.String => ParseStringValue(ref reader),
            JsonTokenType.Number => reader.GetDecimal(),
            var _ => null
        };

    private static decimal? ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        if (reader.ValueSpan.IsEmpty)
            return null;

        return decimal.TryParse(reader.ValueSpan, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
#else
        var stringValue = reader.GetString();
        return string.IsNullOrWhiteSpace(stringValue) || !decimal.TryParse(stringValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? null
            : value;
#endif
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteNumberValue(value.Value);
    }
}