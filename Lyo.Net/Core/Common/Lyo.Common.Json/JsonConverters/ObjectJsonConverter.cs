using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Conversion;

namespace Lyo.Common.Json.JsonConverters;

/// <summary>
/// STJ adapter over <see cref="TypeConversion.FromJsonElement(in JsonElement)" /> so <c>object</c> properties and collection items
/// round-trip as CLR primitives instead of <see cref="JsonElement" />.
/// </summary>
public sealed class ObjectJsonConverter : JsonConverter<object>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return TypeConversion.FromJsonElement(doc.RootElement);
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
