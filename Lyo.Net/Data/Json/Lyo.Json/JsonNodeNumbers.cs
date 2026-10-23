using System.Text.Json.Nodes;

namespace Lyo.Json;

/// <summary>Helpers for a <see cref="JsonNode" /> tree shared by the document libraries.</summary>
public static class JsonNodeNumbers
{
    /// <summary>Replaces a string that is a canonical number with a JSON number. "01" stays a string.</summary>
    public static void Coerce(JsonNode? node) => JsonNumberCoercion.Apply(node);
}