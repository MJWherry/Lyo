using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.Common.JsonConverters;

public class StringDateTimeConverter(params string[] formats) : JsonConverter<DateTime>
{
    private readonly string[] _formats = formats;

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch {
            JsonTokenType.String => ParseStringValue(ref reader),
            var _ => throw new JsonException($"Expected a string for DateTime value, got {reader.TokenType}.")
        };

    private DateTime ParseStringValue(ref Utf8JsonReader reader)
    {
#if NET9_0_OR_GREATER
        var str = reader.GetString();
        if (string.IsNullOrEmpty(str))
            throw new JsonException("Cannot parse DateTime from null or empty string.");

        foreach (var format in _formats) {
            if (str.Length == GetFormatLength(format) && DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
        }

        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fallback))
            return fallback;

        throw new JsonException($"Could not parse DateTime: '{str}'");
#else
        var str = reader.GetString();
        if (string.IsNullOrEmpty(str))
            throw new JsonException("Cannot parse DateTime from null or empty string.");

        foreach (var format in _formats) {
            if (str!.Length == GetFormatLength(format) && DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
        }

        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var fallback))
            return fallback;

        throw new JsonException($"Could not parse DateTime: '{str}'");
#endif
    }

    private static int GetFormatLength(string format)
        =>
            // Calculate expected length based on format string
            format switch {
                "yyyy-MM-dd" => 10,
                "MM/dd/yyyy" => 10,
                "dd/MM/yyyy" => 10,
                "yyyyMMdd" => 8,
                "yyyy-MM-ddTHH:mm:ss" => 19,
                "yyyy-MM-ddTHH:mm:ss.fff" => 23,
                "yyyy-MM-ddTHH:mm:ss.fffZ" => 24,
                "HH:mm:ss" => 8,
                "HH:mm" => 5,
                var _ => format.Length // Fallback: assume format length equals expected string length
            };

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => throw
            // we do not know what property we are serializing, and so we cannot get the property name or format that it should 
            // be serialized to. May need to specify converters per format instead of a generic one
            new("Deserializing accepts multiple formats, serializing does not.");
}