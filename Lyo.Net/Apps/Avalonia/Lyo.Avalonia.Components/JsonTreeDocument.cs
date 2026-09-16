using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lyo.Desktop.Components;

/// <summary>Kind of value a <see cref="JsonTreeRow" /> shows. Objects and arrays are containers.</summary>
public enum JsonTreeKind
{
    /// <summary>A JSON object.</summary>
    Object,

    /// <summary>A JSON array.</summary>
    Array,

    /// <summary>A JSON string.</summary>
    String,

    /// <summary>A JSON number.</summary>
    Number,

    /// <summary>JSON true or false.</summary>
    Boolean,

    /// <summary>JSON null.</summary>
    Null
}

/// <summary>One visible row in a <see cref="JsonTreeDocument" />.</summary>
public sealed class JsonTreeRow
{
    /// <summary>Path from the root. Empty for the root. Children use dotted indexes such as <c>0.1</c>.</summary>
    public required string Path { get; init; }

    /// <summary>Property name, or <c>[0]</c> for an array item. Empty for the root.</summary>
    public required string Key { get; init; }

    /// <summary>Scalar text, or a short container summary.</summary>
    public required string Value { get; init; }

    /// <summary>Value kind. The viewer colors scalars from this.</summary>
    public required JsonTreeKind Kind { get; init; }

    /// <summary>Nesting depth. The root is 0.</summary>
    public required int Depth { get; init; }

    /// <summary>True when the node has children.</summary>
    public required bool Expandable { get; init; }

    /// <summary>True when those children are listed.</summary>
    public required bool Expanded { get; init; }

    /// <summary>True for the active search match.</summary>
    public required bool SearchHit { get; init; }

    /// <summary>Clipboard text. A string is the string itself. Anything else is indented JSON.</summary>
    public required string CopyText { get; init; }
}

/// <summary>
/// Read-only JSON tree. The root starts expanded and deeper containers start collapsed.
/// Search expands the ancestors of the active match so that row is visible.
/// </summary>
public sealed class JsonTreeDocument
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };
    private readonly Node _root;
    private readonly HashSet<string> _expanded = new(StringComparer.Ordinal) { "" };
    private List<Node> _matches = [];
    private string _query = "";
    private int _matchIndex = -1;

    private JsonTreeDocument(Node root) => _root = root;

    /// <summary>1-based index of the active match, or 0 when there is none.</summary>
    public int SearchCurrent => _matchIndex < 0 ? 0 : _matchIndex + 1;

    /// <summary>How many nodes match the current search.</summary>
    public int SearchTotal => _matches.Count;

    /// <summary>Rows for containers that are expanded, plus the root. A collapsed node omits its children.</summary>
    public IReadOnlyList<JsonTreeRow> VisibleRows
    {
        get {
            var rows = new List<JsonTreeRow>();
            Walk(_root, rows);
            return rows;
        }
    }

    /// <summary>Parses <paramref name="text" />. Returns false for blank text and for text that is not JSON.</summary>
    public static bool TryParse(string? text, out JsonTreeDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        try {
            var node = JsonNode.Parse(text);
            document = new(Build(node, "", "", 0));
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    /// <summary>Opens or closes the container at <paramref name="path" />.</summary>
    public void Toggle(string path)
    {
        if (_expanded.Contains(path))
            _expanded.Remove(path);
        else
            _expanded.Add(path);
    }

    /// <summary>Opens every container, including the root.</summary>
    public void ExpandAll()
    {
        foreach (var node in All()) {
            if (node.Expandable)
                _expanded.Add(node.Path);
        }
    }

    /// <summary>Closes every container. Only the root row stays visible.</summary>
    public void CollapseAll() => _expanded.Clear();

    /// <summary>Finds nodes whose key or scalar value contains <paramref name="text" />. The first match becomes active and its ancestors open.</summary>
    public void Search(string? text)
    {
        _query = text?.Trim() ?? "";
        _matches = string.IsNullOrEmpty(_query) ? [] : All().Where(Matches).ToList();
        _matchIndex = _matches.Count == 0 ? -1 : 0;
        RevealCurrent();
    }

    /// <summary>Moves the active match forward and opens its ancestors.</summary>
    public void NextMatch() => Move(1);

    /// <summary>Moves the active match backward and opens its ancestors.</summary>
    public void PreviousMatch() => Move(-1);

    private void Move(int delta)
    {
        if (_matches.Count == 0)
            return;
        _matchIndex = (_matchIndex + delta + _matches.Count) % _matches.Count;
        RevealCurrent();
    }

    private void RevealCurrent()
    {
        if (_matchIndex < 0)
            return;
        _expanded.Add("");
        var path = _matches[_matchIndex].Path;
        var dot = path.LastIndexOf('.');
        while (dot >= 0) {
            path = path[..dot];
            _expanded.Add(path);
            dot = path.LastIndexOf('.');
        }
    }

    private bool Matches(Node node)
    {
        if (node.Key.Contains(_query, StringComparison.OrdinalIgnoreCase))
            return true;
        if (node.Kind is JsonTreeKind.Object or JsonTreeKind.Array)
            return false;
        return node.Value.Contains(_query, StringComparison.OrdinalIgnoreCase);
    }

    private void Walk(Node node, List<JsonTreeRow> rows)
    {
        var expanded = node.Expandable && _expanded.Contains(node.Path);
        rows.Add(new() {
            Path = node.Path,
            Key = node.Key,
            Value = node.Value,
            Kind = node.Kind,
            Depth = node.Depth,
            Expandable = node.Expandable,
            Expanded = expanded,
            SearchHit = _matchIndex >= 0 && ReferenceEquals(node, _matches[_matchIndex]),
            CopyText = node.CopyText
        });
        if (!expanded)
            return;
        foreach (var child in node.Children)
            Walk(child, rows);
    }

    private IEnumerable<Node> All()
    {
        var stack = new Stack<Node>();
        stack.Push(_root);
        while (stack.Count > 0) {
            var node = stack.Pop();
            yield return node;
            for (var i = node.Children.Count - 1; i >= 0; i--)
                stack.Push(node.Children[i]);
        }
    }

    private static Node Build(JsonNode? node, string key, string path, int depth)
    {
        var kind = KindOf(node);
        var built = new Node {
            Path = path,
            Key = key,
            Kind = kind,
            Depth = depth,
            Value = Display(node, kind),
            CopyText = CopyOf(node)
        };
        if (node is JsonObject obj) {
            var index = 0;
            foreach (var pair in obj) {
                built.Children.Add(Build(pair.Value, pair.Key, ChildPath(path, index), depth + 1));
                index++;
            }
        }
        else if (node is JsonArray array) {
            for (var i = 0; i < array.Count; i++)
                built.Children.Add(Build(array[i], $"[{i}]", ChildPath(path, i), depth + 1));
        }

        return built;
    }

    private static string ChildPath(string path, int index) => string.IsNullOrEmpty(path) ? index.ToString() : path + "." + index;

    private static JsonTreeKind KindOf(JsonNode? node) => node switch {
        JsonObject => JsonTreeKind.Object,
        JsonArray => JsonTreeKind.Array,
        JsonValue value when value.GetValueKind() == System.Text.Json.JsonValueKind.String => JsonTreeKind.String,
        JsonValue value when value.GetValueKind() == System.Text.Json.JsonValueKind.Number => JsonTreeKind.Number,
        JsonValue value when value.GetValueKind() is System.Text.Json.JsonValueKind.True or System.Text.Json.JsonValueKind.False => JsonTreeKind.Boolean,
        _ => JsonTreeKind.Null
    };

    private static string Display(JsonNode? node, JsonTreeKind kind) => kind switch {
        JsonTreeKind.Object => "{" + (node as JsonObject)?.Count + "}",
        JsonTreeKind.Array => "[" + (node as JsonArray)?.Count + "]",
        JsonTreeKind.String => node?.GetValue<string>() ?? "",
        JsonTreeKind.Number => node?.ToJsonString() ?? "",
        JsonTreeKind.Boolean => node?.ToJsonString() ?? "",
        _ => "null"
    };

    private static string CopyOf(JsonNode? node)
    {
        if (node is JsonValue value && value.GetValueKind() == System.Text.Json.JsonValueKind.String)
            return value.GetValue<string>() ?? "";
        return node?.ToJsonString(Indented) ?? "null";
    }

    private sealed class Node
    {
        public required string Path { get; init; }
        public required string Key { get; init; }
        public required string Value { get; init; }
        public required JsonTreeKind Kind { get; init; }
        public required int Depth { get; init; }
        public required string CopyText { get; init; }
        public List<Node> Children { get; } = [];
        public bool Expandable => Children.Count > 0;
    }
}
