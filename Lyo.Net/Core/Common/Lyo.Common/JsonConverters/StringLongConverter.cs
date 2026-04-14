using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

public class StringLongConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.String => ParseStringValue(ref reader),
            JsonTokenType.Number => reader.GetInt32(),
            var _ => 0
        };

    private static long ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        return long.TryParse(reader.ValueSpan, out var i) ? i : throw new();
#else
        var stringValue = reader.GetString();
        return long.TryParse(stringValue, out var i) ? i : throw new();
#endif
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}