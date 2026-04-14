using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
#if NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace Lyo.Common.JsonConverters;

public class StringEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) {
#if NET9_0_OR_GREATER
            // Use ValueSpan for .NET 9.0+ for better performance - avoid string allocation
            var valueSpan = reader.ValueSpan;

            // Convert UTF-8 bytes to string on stack if small enough
            if (valueSpan.Length <= 128) {
                Span<char> charBuffer = stackalloc char[128];
                var charCount = Encoding.UTF8.GetChars(valueSpan, charBuffer);
                ReadOnlySpan<char> enumName = charBuffer.Slice(0, charCount);
                if (Enum.TryParse<TEnum>(enumName, true, out var valueFromSpan))
                    return valueFromSpan;

                throw new JsonException($"Unable to convert \"{enumName.ToString()}\" to enum {typeof(TEnum)}.");
            }

            // Fallback to string allocation for very long values
            var enumText = reader.GetString();
            if (Enum.TryParse<TEnum>(enumText, true, out var value))
                return value;

            throw new JsonException($"Unable to convert \"{enumText}\" to enum {typeof(TEnum)}.");
#else
            // Fallback for .NET Standard 2.0
            var enumText = reader.GetString();
            if (Enum.TryParse<TEnum>(enumText, true, out var value))
                return value;

            throw new JsonException($"Unable to convert \"{enumText}\" to enum {typeof(TEnum)}.");
#endif
        }

        if (reader.TokenType == JsonTokenType.Number) {
            if (reader.TryGetInt32(out var intValue)) {
#if NET9_0_OR_GREATER
                // Use more efficient enum validation in .NET 9.0+
                if (Enum.IsDefined(Unsafe.As<int, TEnum>(ref intValue)))
                    return Unsafe.As<int, TEnum>(ref intValue);
#else
                // Fallback for .NET Standard 2.0
                if (Enum.IsDefined(typeof(TEnum), intValue))
                    return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
#endif
                throw new JsonException($"Value {intValue} is not defined for enum type {typeof(TEnum)}.");
            }
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
#if NET9_0_OR_GREATER
        // Use span-based writing for better performance in .NET 9.0+
        Span<char> buffer = stackalloc char[64]; // Most enum names fit in 64 chars
        if (Enum.TryFormat(value, buffer, out var charsWritten))
            writer.WriteStringValue(buffer.Slice(0, charsWritten));
        else {
            // Fallback if buffer is too small
            writer.WriteStringValue(value.ToString());
        }
#else
        // Fallback for .NET Standard 2.0
        writer.WriteStringValue(value.ToString());
#endif
    }
}