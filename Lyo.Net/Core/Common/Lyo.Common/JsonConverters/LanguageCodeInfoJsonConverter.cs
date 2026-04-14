using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Records;

namespace Lyo.Common.JsonConverters;

/// <summary>JSON converter for LanguageCodeInfo that serializes/deserializes using BCP 47 code.</summary>
public class LanguageCodeInfoJsonConverter : JsonConverter<LanguageCodeInfo>
{
    public override LanguageCodeInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return LanguageCodeInfo.Unknown;

        if (reader.TokenType == JsonTokenType.String) {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return LanguageCodeInfo.Unknown;

            // Try BCP 47 first, then ISO 639-1, then ISO 639-3
            var result = LanguageCodeInfo.FromBcp47(value);
            if (result != LanguageCodeInfo.Unknown)
                return result;

            result = LanguageCodeInfo.FromIso6391(value);
            if (result != LanguageCodeInfo.Unknown)
                return result;

            result = LanguageCodeInfo.FromIso6393(value);
            return result;
        }

        return LanguageCodeInfo.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, LanguageCodeInfo value, JsonSerializerOptions options)
    {
        if (value == null || value == LanguageCodeInfo.Unknown) {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Bcp47);
    }
}

/// <summary>JSON converter for nullable LanguageCodeInfo.</summary>
public class NullableLanguageCodeInfoJsonConverter : JsonConverter<LanguageCodeInfo?>
{
    public override LanguageCodeInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var converter = new LanguageCodeInfoJsonConverter();
        return converter.Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, LanguageCodeInfo? value, JsonSerializerOptions options)
    {
        if (value == null) {
            writer.WriteNullValue();
            return;
        }

        var converter = new LanguageCodeInfoJsonConverter();
        converter.Write(writer, value, options);
    }
}