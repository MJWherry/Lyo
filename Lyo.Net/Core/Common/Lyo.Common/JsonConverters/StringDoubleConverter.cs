using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

public class StringDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.String => ParseStringValue(ref reader),
            JsonTokenType.Number => reader.GetInt32(),
            var _ => 0
        };

    private static double ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        return int.TryParse(reader.ValueSpan, out var i) ? i : throw new();
#else
        var stringValue = reader.GetString();
        return double.TryParse(stringValue, out var i) ? i : throw new();
#endif
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
}