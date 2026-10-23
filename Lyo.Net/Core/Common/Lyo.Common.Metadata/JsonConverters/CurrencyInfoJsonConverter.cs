using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Extensions;
using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Metadata.JsonConverters;

/// <summary>System.Text.Json converter that stores <see cref="CurrencyInfo" /> as its ISO 4217 alpha-3 code.</summary>
public class CurrencyInfoJsonConverter : JsonConverter<CurrencyInfo>
{
    public override CurrencyInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return CurrencyInfo.Unknown;

        if (reader.TokenType != JsonTokenType.String)
            return CurrencyInfo.Unknown;

        var value = reader.GetString();
        if (value.IsNullOrWhitespace())
            return CurrencyInfo.Unknown;

        return CurrencyInfo.FromCode(value);
    }

    public override void Write(Utf8JsonWriter writer, CurrencyInfo value, JsonSerializerOptions options)
    {
        if (value == CurrencyInfo.Unknown) {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Code);
    }
}

/// <summary>System.Text.Json converter for a nullable <see cref="CurrencyInfo" />.</summary>
public class NullableCurrencyInfoJsonConverter : JsonConverter<CurrencyInfo?>
{
    public override CurrencyInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var converter = new CurrencyInfoJsonConverter();
        return converter.Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, CurrencyInfo? value, JsonSerializerOptions options)
    {
        if (value == null) {
            writer.WriteNullValue();
            return;
        }

        var converter = new CurrencyInfoJsonConverter();
        converter.Write(writer, value, options);
    }
}