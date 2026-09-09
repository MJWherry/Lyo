using System.Text.Json;
using System.Text.Json.Serialization;
#if NET9_0_OR_GREATER
using System.Text;
using System.Runtime.CompilerServices;
#endif

namespace Lyo.Common.Json.JsonConverters;

public class StringEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) {
#if NET9_0_OR_GREATER
            // Prefer ValueSpan on .NET 9+ so we skip allocating a string
            var valueSpan = reader.ValueSpan;

            // Decode UTF-8 onto the stack when the token is short enough
            if (valueSpan.Length <= 128) {
                Span<char> charBuffer = stackalloc char[128];
                var charCount = Encoding.UTF8.GetChars(valueSpan, charBuffer);
                ReadOnlySpan<char> enumName = charBuffer.Slice(0, charCount);
                if (Enum.TryParse<TEnum>(enumName, true, out var valueFromSpan))
                    return valueFromSpan;

                throw new JsonException($"Unable to convert \"{enumName.ToString()}\" to enum {typeof(TEnum)}.");
            }

            // Heap-allocate when the token is too long for the stack buffer
            var enumText = reader.GetString();
            if (Enum.TryParse<TEnum>(enumText, true, out var value))
                return value;

            throw new JsonException($"Unable to convert \"{enumText}\" to enum {typeof(TEnum)}.");
#else
            // netstandard2.0 path: parse from an allocated string
            var enumText = reader.GetString();
            if (Enum.TryParse<TEnum>(enumText, true, out var value))
                return value;

            throw new JsonException($"Unable to convert \"{enumText}\" to enum {typeof(TEnum)}.");
#endif
        }

        if (reader.TokenType == JsonTokenType.Number) {
            if (reader.TryGetInt32(out var intValue)) {
#if NET9_0_OR_GREATER
                // .NET 9+ can validate without boxing the enum value
                if (Enum.IsDefined(Unsafe.As<int, TEnum>(ref intValue)))
                    return Unsafe.As<int, TEnum>(ref intValue);
#else
                // netstandard2.0 path: boxed IsDefined check
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
        // Write via a stack span on .NET 9+ (avoids ToString for typical names)
        Span<char> buffer = stackalloc char[64]; // 64 chars covers typical enum names
        if (Enum.TryFormat(value, buffer, out var charsWritten))
            writer.WriteStringValue(buffer.Slice(0, charsWritten));
        else {
            // Name did not fit the stack buffer; allocate via ToString
            writer.WriteStringValue(value.ToString());
        }
#else
        // netstandard2.0 path: write the enum name as a string
        writer.WriteStringValue(value.ToString());
#endif
    }
}