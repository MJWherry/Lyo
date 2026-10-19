using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Compression.Models;

namespace Lyo.Compression.JsonConverters;

/// <summary>JSON converter for <see cref="CompressionAlgorithm" /> that serializes and deserializes using the stable algorithm <see cref="CompressionAlgorithm.Name" />.</summary>
public sealed class CompressionAlgorithmJsonConverter : JsonConverter<CompressionAlgorithm?>
{

    public override CompressionAlgorithm? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
            return CompressionAlgorithm.TryFromName(reader.GetString());

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing CompressionAlgorithm.");
    }

    public override void Write(Utf8JsonWriter writer, CompressionAlgorithm? value, JsonSerializerOptions options)
    {
        if (value == null) {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Name);
    }
}