using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

public class StringIntNullableConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.String => ParseStringValue(ref reader),
            JsonTokenType.Number => reader.GetInt32(),
            var _ => 0
        };

    private static int? ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        return int.TryParse(reader.ValueSpan, out var i) ? i : null;
#else
        var stringValue = reader.GetString();
        return int.TryParse(stringValue, out var i) ? i : null;
#endif
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.ToString());
    }
}