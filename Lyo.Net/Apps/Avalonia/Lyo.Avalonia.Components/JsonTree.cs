using System.Text.Json;

namespace Lyo.Desktop.Components;

/// <summary>One node in a <see cref="JsonTree" />. Objects and arrays keep their children.</summary>
public sealed class JsonTreeNode
{
    /// <summary>Text for this node, including the property name when it has one.</summary>
    public required string Label { get; init; }

    /// <summary>Nested properties or array items. Empty for a scalar.</summary>
    public IReadOnlyList<JsonTreeNode> Children { get; init; } = [];
}

/// <summary>Parses a JSON document into a tree a desktop editor can draw.</summary>
public static class JsonTree
{
    /// <summary>Parses <paramref name="text" />. Returns false for blank text and for text that is not JSON.</summary>
    public static bool TryParse(string? text, out IReadOnlyList<JsonTreeNode> nodes)
    {
        nodes = [];
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try {
            using var document = JsonDocument.Parse(text);
            nodes = [Node(null, document.RootElement)];
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    private static JsonTreeNode Node(string? name, JsonElement value)
    {
        var children = new List<JsonTreeNode>();
        switch (value.ValueKind) {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                    children.Add(Node(property.Name, property.Value));

                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in value.EnumerateArray())
                    children.Add(Node(index++.ToString(), item));

                break;
        }

        return new() { Label = Label(name, value, children), Children = children };
    }

    private static string Label(string? name, JsonElement value, List<JsonTreeNode> children)
    {
        var prefix = string.IsNullOrEmpty(name) ? "" : name + ": ";
        if (value.ValueKind == JsonValueKind.Array)
            return prefix + (children.Count == 0 ? "[]" : "[" + children.Count + "]");

        if (value.ValueKind == JsonValueKind.Object) {
            if (children.Count == 0)
                return prefix + "{}";

            var names = children.Select(child => child.Label.Split(':')[0].Trim());
            return prefix + "{" + string.Join(", ", names) + "}";
        }

        return prefix + Scalar(value);
    }

    private static string Scalar(JsonElement value)
        => value.ValueKind switch {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Number => value.GetRawText(),
            var _ => value.GetRawText()
        };
}