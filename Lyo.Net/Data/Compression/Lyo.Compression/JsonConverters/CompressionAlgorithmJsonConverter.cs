using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Compression.Models;

namespace Lyo.Compression.JsonConverters;

/// <summary>JSON converter for <see cref="CompressionAlgorithm" /> that serializes/deserializes using the stable algorithm <see cref="CompressionAlgorithm.Name" />.</summary>
public sealed class CompressionAlgorithmJsonConverter : JsonConverter<CompressionAlgorithm>
{
    private const string NameProperty = "Name";

    public override CompressionAlgorithm? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
            return CompressionAlgorithm.TryFromName(reader.GetString());

        if (reader.TokenType == JsonTokenType.StartObject)
            return ReadLegacyObject(ref reader);

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing CompressionAlgorithm.");
    }

    public override void Write(Utf8JsonWriter writer, CompressionAlgorithm value, JsonSerializerOptions options)
    {
        if (value == null) {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Name);
    }

    private static CompressionAlgorithm? ReadLegacyObject(ref Utf8JsonReader reader)
    {
        string? name = null;
        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName) {
                reader.Skip();
                continue;
            }

            var propertyName = reader.GetString();
            reader.Read();
            if (string.Equals(propertyName, NameProperty, StringComparison.OrdinalIgnoreCase))
                name = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            else
                reader.Skip();
        }

        return CompressionAlgorithm.TryFromName(name);
    }
}
