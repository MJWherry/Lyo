using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

public class StringIntBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.Null => throw new(),
            JsonTokenType.String => ParseStringValue(ref reader),
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean(),
            JsonTokenType.Number => reader.GetInt32() != 0,
            var _ => throw new()
        };

    private static bool ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        var span = reader.ValueSpan;
        return span.Length == 1 && span[0] == (byte)'1';
#else
        var stringValue = reader.GetString();
        return stringValue == "1";
#endif
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteStringValue(Convert.ToBoolean(value).ToString());
}