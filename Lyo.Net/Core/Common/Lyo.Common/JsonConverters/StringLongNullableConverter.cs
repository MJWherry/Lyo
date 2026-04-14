using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

public class StringLongNullableConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.String => ParseStringValue(ref reader),
            JsonTokenType.Number => reader.GetInt32(),
            var _ => 0
        };

    private static long? ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        return long.TryParse(reader.ValueSpan, out var i) ? i : null;
#else
        var stringValue = reader.GetString();
        return long.TryParse(stringValue, out var i) ? i : null;
#endif
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}