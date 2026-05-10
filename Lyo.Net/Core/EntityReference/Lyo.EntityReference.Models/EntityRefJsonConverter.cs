using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lyo.EntityReference.Models;

/// <summary>
/// JSON shape for <see cref="EntityRef"/>: <c>{"entityType":"…","entityId":"…"}</c> (camelCase property names).
/// Register with <see cref="JsonSerializerOptions.Converters"/> for stable HTTP and storage contracts independent of global naming policy.
/// </summary>
public sealed class EntityRefJsonConverter : JsonConverter<EntityRef>
{
    const string EntityTypeName = "entityType";
    const string EntityIdName = "entityId";

    /// <summary>Reads <c>entityType</c> and <c>entityId</c> from a JSON object.</summary>
    /// <exception cref="JsonException">The JSON is not an object or required properties are missing or blank.</exception>
    public override EntityRef Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object for EntityRef.");

        string? entityType = null;
        string? entityId = null;

        while (reader.Read()) {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Unexpected token in EntityRef object.");

            var prop = reader.GetString();
            reader.Read();
            var val = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();

            if (string.Equals(prop, EntityTypeName, StringComparison.Ordinal) || string.Equals(prop, nameof(EntityRef.EntityType), StringComparison.Ordinal))
                entityType = val;
            else if (string.Equals(prop, EntityIdName, StringComparison.Ordinal) || string.Equals(prop, nameof(EntityRef.EntityId), StringComparison.Ordinal))
                entityId = val;
            else
                reader.Skip();
        }

        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
            throw new JsonException("EntityRef JSON must include non-empty entityType and entityId.");

        return new EntityRef(entityType!, entityId!);
    }

    /// <summary>Writes <c>entityType</c> and <c>entityId</c> as camelCase properties.</summary>
    public override void Write(Utf8JsonWriter writer, EntityRef value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(EntityTypeName, value.EntityType);
        writer.WriteString(EntityIdName, value.EntityId);
        writer.WriteEndObject();
    }
}
