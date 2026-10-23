using System.Globalization;
using System.Text.Json;
using Lyo.Algorithms.Models;
using Lyo.Algorithms.Models.Request;
using Lyo.Algorithms.Models.Response;
using Lyo.Exceptions;

namespace Lyo.Algorithms.Web.Components;

/// <summary>Which control edits a context or scorer value for an alias.</summary>
public enum AlgorithmValueKind
{
    /// <summary>A number.</summary>
    Number = 0,

    /// <summary>A boolean.</summary>
    Switch = 1,

    /// <summary>Free text, including strings and host enum names.</summary>
    Text = 2,

    /// <summary>A date or offset.</summary>
    Date = 3,

    /// <summary>A duration.</summary>
    Duration = 4,

    /// <summary>A one-level array. The text is a JSON array.</summary>
    List = 5
}

/// <summary>One declared parameter or call type.</summary>
public sealed class AlgorithmTypeRow
{
    /// <summary>Identifier, or a catalog key for a call.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Alias such as <c>decimal</c> or <c>datetime[]</c>.</summary>
    public string Alias { get; set; } = "double";
}

/// <summary>One discrete choice. <see cref="Value" /> is text until the payload writer types it.</summary>
public sealed class AlgorithmMapEntry
{
    /// <summary>Matched text.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Result text.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>One half-open band. Bounds and the result are text until the payload writer types them.</summary>
public sealed class AlgorithmBandEntry
{
    /// <summary>Inclusive start.</summary>
    public string Min { get; set; } = "0";

    /// <summary>Exclusive end.</summary>
    public string Max { get; set; } = "1";

    /// <summary>Result while the input is in this band.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>One composite child. A ref and an inline kind cannot both be set.</summary>
public sealed class AlgorithmChildDraft
{
    /// <summary>Name used by <c>score("name")</c>.</summary>
    public string? Key { get; set; }

    /// <summary>Catalog key. Latest revision.</summary>
    public string? Ref { get; set; }

    /// <summary>Inline scorer kind. Null when <see cref="Ref" /> is set.</summary>
    public AlgorithmKind? Kind { get; set; }

    /// <summary>Weighted-sum coefficient. Omitted from JSON when it is 1.</summary>
    public string Weight { get; set; } = "1";

    /// <summary>Inline body. Ignored when <see cref="Ref" /> is set.</summary>
    public AlgorithmDraft? Inline { get; set; }
}

/// <summary>One context bag row.</summary>
public sealed class AlgorithmContextRow
{
    /// <summary>Expression identifier.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Alias used to encode <see cref="Text" />.</summary>
    public string Alias { get; set; } = "double";

    /// <summary>Author text. Arrays are JSON arrays.</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>Editable revision. <see cref="ToPayload" /> writes the catalog JSON, including the type binding.</summary>
public sealed class AlgorithmDraft
{
    /// <summary>Catalog key for the open definition. <c>preview</c> until one is chosen.</summary>
    public string Key { get; set; } = "preview";

    /// <summary>Display name stored with the next revision.</summary>
    public string? Name { get; set; }

    /// <summary>Description stored with the next revision.</summary>
    public string? Description { get; set; }

    /// <summary>Revision kind.</summary>
    public AlgorithmKind Kind { get; set; } = AlgorithmKind.Expression;

    /// <summary>Expression source, or the startup key when <see cref="Kind" /> is named.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Result alias. <c>double</c> is omitted from the payload.</summary>
    public string Result { get; set; } = "double";

    /// <summary>Numeric input alias for a band or a line. Null omits <c>inputType</c>.</summary>
    public string? InputType { get; set; }

    /// <summary>Declared identifiers.</summary>
    public List<AlgorithmTypeRow> Parameters { get; set; } = [];

    /// <summary>Declared <c>Algo</c> / <c>score</c> result types.</summary>
    public List<AlgorithmTypeRow> Calls { get; set; } = [];

    /// <summary>Scorer input identifier.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>Discrete-map miss policy.</summary>
    public MissingKeyPolicy Missing { get; set; } = MissingKeyPolicy.Zero;

    /// <summary>Range-band miss policy.</summary>
    public OutOfRangePolicy OutOfRange { get; set; } = OutOfRangePolicy.Zero;

    /// <summary>Value used when a non-numeric scorer misses and the policy is zero.</summary>
    public string? MissingValue { get; set; }

    /// <summary>Discrete choices.</summary>
    public List<AlgorithmMapEntry> Entries { get; set; } = [];

    /// <summary>Half-open bands.</summary>
    public List<AlgorithmBandEntry> Bands { get; set; } = [];

    /// <summary>Linear input start.</summary>
    public string InputMin { get; set; } = "0";

    /// <summary>Linear input end.</summary>
    public string InputMax { get; set; } = "1";

    /// <summary>Linear result at <see cref="InputMin" />.</summary>
    public string PointsMin { get; set; } = "0";

    /// <summary>Linear result at <see cref="InputMax" />.</summary>
    public string PointsMax { get; set; } = "1";

    /// <summary>When true, linear inputs outside the range stay on the nearest endpoint.</summary>
    public bool Clamp { get; set; }

    /// <summary>How composite children combine.</summary>
    public CombinerKind Combiner { get; set; } = CombinerKind.Sum;

    /// <summary>Formula used when <see cref="Combiner" /> is <see cref="CombinerKind.Formula" />.</summary>
    public string? Formula { get; set; }

    /// <summary>Composite children, in order.</summary>
    public List<AlgorithmChildDraft> Children { get; set; } = [];

    /// <summary>Problems a save or preview would hit. Empty when the draft is ready.</summary>
    public IReadOnlyList<string> Issues() => AlgorithmDraftRules.Issues(this);

    /// <summary>Catalog body. Null when an expression or named revision has no type fields.</summary>
    public string? ToPayload() => AlgorithmDraftRules.WritePayload(this);

    /// <summary>Append body for <see cref="IAlgorithmClient"/>.</summary>
    public AlgorithmRevisionReq ToRevision() => new() { Kind = Kind, Source = Source ?? string.Empty, Payload = ToPayload(), Name = Name, Description = Description };

    /// <summary>Reads a stored revision into a draft. Unknown JSON fields are ignored.</summary>
    public static AlgorithmDraft FromRevision(AlgorithmRevisionRes revision)
    {
        ArgumentHelpers.ThrowIfNull(revision);
        var draft = new AlgorithmDraft { Kind = revision.Kind, Source = revision.Source ?? string.Empty };
        if (string.IsNullOrWhiteSpace(revision.Payload))
            return draft;

        using var document = JsonDocument.Parse(revision.Payload);
        AlgorithmDraftRules.Read(draft, document.RootElement);
        return draft;
    }

    /// <summary>Sets or replaces the parameter alias for <paramref name="name" />.</summary>
    public void SetParameter(string name, string alias)
    {
        var row = Parameters.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        if (row is null)
            Parameters.Add(new() { Name = name, Alias = alias });
        else
            row.Alias = alias;
    }
}

/// <summary>Payload, context JSON, and identifier rules shared by the Blazor and tests.</summary>
public static class AlgorithmDraftRules
{
    /// <summary>Built-in aliases. A trailing <c>[]</c> is a one-level list of the same alias. Nested arrays are rejected.</summary>
    public static IReadOnlyList<string> Aliases { get; } = [
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal", "string", "datetime", "datetimeoffset", "timespan"
    ];

    private static readonly HashSet<string> Numeric = new(StringComparer.OrdinalIgnoreCase) {
        "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal"
    };

    /// <summary>True when <paramref name="name" /> can be an expression identifier.</summary>
    public static bool IsIdentifier(string? name) => name is not null && AlgorithmContextBag.IsIdentifier(name);

    /// <summary><c>string</c> for a <c>_key</c> name, otherwise <c>double</c>.</summary>
    public static string DefaultAlias(string name) => name.EndsWith("_key", StringComparison.Ordinal) ? "string" : "double";

    /// <summary>Control for <paramref name="alias" />. A list uses the element alias's control plus a JSON array.</summary>
    public static AlgorithmValueKind EditorKind(string? alias)
    {
        var text = (alias ?? "double").Trim();
        if (text.EndsWith("[]", StringComparison.Ordinal))
            return AlgorithmValueKind.List;

        if (text.Equals("bool", StringComparison.OrdinalIgnoreCase))
            return AlgorithmValueKind.Switch;

        if (text.Equals("datetime", StringComparison.OrdinalIgnoreCase) || text.Equals("datetimeoffset", StringComparison.OrdinalIgnoreCase))
            return AlgorithmValueKind.Date;

        if (text.Equals("timespan", StringComparison.OrdinalIgnoreCase))
            return AlgorithmValueKind.Duration;

        if (Numeric.Contains(text))
            return AlgorithmValueKind.Number;

        return AlgorithmValueKind.Text;
    }

    /// <summary>True when a sum, a weighted sum, a line, or a numeric input must stay numeric.</summary>
    public static bool IsNumericAlias(string? alias) => alias is not null && Numeric.Contains(alias.Trim());

    /// <summary>Inserts <paramref name="token" /> over the caret range.</summary>
    public static string Insert(string? source, int start, int end, string token)
    {
        var text = source ?? string.Empty;
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        return string.Concat(text.AsSpan(0, start), token, text.AsSpan(end));
    }

    /// <summary>Messages for this draft. An empty list means the payload shape is acceptable.</summary>
    public static IReadOnlyList<string> Issues(AlgorithmDraft draft)
    {
        var issues = new List<string>();
        if (!string.IsNullOrWhiteSpace(draft.Result) && draft.Result.EndsWith("[][]", StringComparison.Ordinal))
            issues.Add("Nested arrays are not supported.");

        if (draft.Kind is AlgorithmKind.Expression or AlgorithmKind.Named && string.IsNullOrWhiteSpace(draft.Source))
            issues.Add("Source is required.");

        if (draft.Kind is AlgorithmKind.Linear or AlgorithmKind.RangeBand && !string.IsNullOrWhiteSpace(draft.InputType) && !IsNumericAlias(draft.InputType))
            issues.Add("Input type must be numeric.");

        if (draft.Kind == AlgorithmKind.Linear && !IsNumericAlias(draft.Result))
            issues.Add("A linear result must be numeric.");

        if (draft.Kind == AlgorithmKind.Composite && draft.Combiner != CombinerKind.Formula && !IsNumericAlias(draft.Result))
            issues.Add("Sum and weighted sum results must be numeric.");

        if (NeedsMissing(draft) && string.IsNullOrWhiteSpace(draft.MissingValue))
            issues.Add("A non-numeric result needs missingValue when the missing policy is zero.");

        if (draft.Kind == AlgorithmKind.Composite) {
            if (draft.Children.Count == 0)
                issues.Add("Composite children are required.");

            if (draft.Combiner == CombinerKind.Formula && string.IsNullOrWhiteSpace(draft.Formula))
                issues.Add("Composite formula is required.");

            foreach (var child in draft.Children)
                ChildIssues(child, issues);
        }

        foreach (var row in draft.Parameters) {
            if (!IsIdentifier(row.Name))
                issues.Add($"'{row.Name}' is not an expression identifier.");
        }

        return issues;
    }

    /// <summary>Catalog JSON, or null when there is nothing to store.</summary>
    public static string? WritePayload(AlgorithmDraft draft)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) {
            writer.WriteStartObject();
            var wrote = WriteTypes(writer, draft);
            wrote |= WriteBody(writer, draft);
            writer.WriteEndObject();
            writer.Flush();
            if (!wrote)
                return null;
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static void Read(AlgorithmDraft draft, JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            return;

        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.String)
            draft.Result = result.GetString() ?? "double";

        if (root.TryGetProperty("inputType", out var inputType) && inputType.ValueKind == JsonValueKind.String)
            draft.InputType = inputType.GetString();

        draft.Parameters = ReadMap(root, "parameters");
        draft.Calls = ReadMap(root, "calls");
        if (root.TryGetProperty("input", out var input) && input.ValueKind == JsonValueKind.String)
            draft.Input = input.GetString() ?? string.Empty;

        if (root.TryGetProperty("missing", out var missing) && missing.ValueKind == JsonValueKind.String && Enum.TryParse<MissingKeyPolicy>(missing.GetString(), true, out var miss))
            draft.Missing = miss;

        if (root.TryGetProperty("outOfRange", out var range) && range.ValueKind == JsonValueKind.String && Enum.TryParse<OutOfRangePolicy>(range.GetString(), true, out var policy))
            draft.OutOfRange = policy;

        if (root.TryGetProperty("missingValue", out var missingValue))
            draft.MissingValue = TextOf(missingValue);

        if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array) {
            draft.Entries = [];
            foreach (var entry in entries.EnumerateArray())
                draft.Entries.Add(new() { Key = entry.TryGetProperty("key", out var key) ? key.GetString() ?? string.Empty : string.Empty, Value = entry.TryGetProperty("points", out var points) ? TextOf(points) : string.Empty });
        }

        if (root.TryGetProperty("bands", out var bands) && bands.ValueKind == JsonValueKind.Array) {
            draft.Bands = [];
            foreach (var band in bands.EnumerateArray()) {
                draft.Bands.Add(
                    new() {
                        Min = band.TryGetProperty("min", out var min) ? TextOf(min) : "0",
                        Max = band.TryGetProperty("max", out var max) ? TextOf(max) : "1",
                        Value = band.TryGetProperty("points", out var points) ? TextOf(points) : string.Empty
                    });
            }
        }

        if (root.TryGetProperty("inputMin", out var inputMin))
            draft.InputMin = TextOf(inputMin);

        if (root.TryGetProperty("inputMax", out var inputMax))
            draft.InputMax = TextOf(inputMax);

        if (root.TryGetProperty("pointsMin", out var pointsMin))
            draft.PointsMin = TextOf(pointsMin);

        if (root.TryGetProperty("pointsMax", out var pointsMax))
            draft.PointsMax = TextOf(pointsMax);

        if (root.TryGetProperty("clamp", out var clamp) && clamp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            draft.Clamp = clamp.GetBoolean();

        if (root.TryGetProperty("combiner", out var combiner) && combiner.ValueKind == JsonValueKind.String && Enum.TryParse<CombinerKind>(combiner.GetString(), true, out var kind))
            draft.Combiner = kind;

        if (root.TryGetProperty("formula", out var formula) && formula.ValueKind == JsonValueKind.String)
            draft.Formula = formula.GetString();

        if (root.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array) {
            draft.Children = [];
            foreach (var child in children.EnumerateArray())
                draft.Children.Add(ReadChild(child));
        }
    }

    private static bool NeedsMissing(AlgorithmDraft draft)
    {
        if (IsNumericAlias(draft.Result))
            return false;

        if (draft.Kind == AlgorithmKind.DiscreteMap && draft.Missing == MissingKeyPolicy.Zero)
            return true;

        return draft.Kind == AlgorithmKind.RangeBand && draft.OutOfRange == OutOfRangePolicy.Zero;
    }

    private static void ChildIssues(AlgorithmChildDraft child, List<string> issues)
    {
        var hasRef = !string.IsNullOrWhiteSpace(child.Ref);
        var hasInline = child.Kind is not null;
        if (hasRef == hasInline)
            issues.Add(hasRef ? "Composite child needs a ref or an inline kind, not both." : "Composite child needs a ref or an inline kind.");

        if (hasInline && !IsIdentifier(child.Key))
            issues.Add("Inline composite child key must be an identifier.");
    }

    private static bool WriteTypes(Utf8JsonWriter writer, AlgorithmDraft draft)
    {
        var wrote = false;
        if (!string.IsNullOrWhiteSpace(draft.Result) && !draft.Result.Equals("double", StringComparison.OrdinalIgnoreCase)) {
            writer.WriteString("result", draft.Result.Trim());
            wrote = true;
        }

        if (!string.IsNullOrWhiteSpace(draft.InputType)) {
            writer.WriteString("inputType", draft.InputType.Trim());
            wrote = true;
        }

        wrote |= WriteMap(writer, "parameters", draft.Parameters);
        wrote |= WriteMap(writer, "calls", draft.Calls);
        return wrote;
    }

    private static bool WriteMap(Utf8JsonWriter writer, string name, List<AlgorithmTypeRow> rows)
    {
        var usable = rows.Where(row => !string.IsNullOrWhiteSpace(row.Name)).ToList();
        if (usable.Count == 0)
            return false;

        writer.WriteStartObject(name);
        foreach (var row in usable)
            writer.WriteString(row.Name.Trim(), string.IsNullOrWhiteSpace(row.Alias) ? "double" : row.Alias.Trim());

        writer.WriteEndObject();
        return true;
    }

    private static bool WriteBody(Utf8JsonWriter writer, AlgorithmDraft draft)
    {
        switch (draft.Kind) {
            case AlgorithmKind.DiscreteMap:
                writer.WriteString("input", draft.Input);
                writer.WriteString("missing", draft.Missing.ToString());
                if (!string.IsNullOrWhiteSpace(draft.MissingValue)) {
                    writer.WritePropertyName("missingValue");
                    WriteLiteral(writer, draft.Result, draft.MissingValue);
                }

                writer.WriteStartArray("entries");
                foreach (var entry in draft.Entries) {
                    writer.WriteStartObject();
                    writer.WriteString("key", entry.Key);
                    writer.WritePropertyName("points");
                    WriteLiteral(writer, draft.Result, entry.Value);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                return true;
            case AlgorithmKind.RangeBand:
                writer.WriteString("input", draft.Input);
                writer.WriteString("outOfRange", draft.OutOfRange.ToString());
                if (!string.IsNullOrWhiteSpace(draft.MissingValue)) {
                    writer.WritePropertyName("missingValue");
                    WriteLiteral(writer, draft.Result, draft.MissingValue);
                }

                writer.WriteStartArray("bands");
                foreach (var band in draft.Bands) {
                    writer.WriteStartObject();
                    writer.WritePropertyName("min");
                    WriteLiteral(writer, draft.InputType ?? "double", band.Min);
                    writer.WritePropertyName("max");
                    WriteLiteral(writer, draft.InputType ?? "double", band.Max);
                    writer.WritePropertyName("points");
                    WriteLiteral(writer, draft.Result, band.Value);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                return true;
            case AlgorithmKind.Linear:
                writer.WriteString("input", draft.Input);
                writer.WritePropertyName("inputMin");
                WriteLiteral(writer, draft.InputType ?? "double", draft.InputMin);
                writer.WritePropertyName("inputMax");
                WriteLiteral(writer, draft.InputType ?? "double", draft.InputMax);
                writer.WritePropertyName("pointsMin");
                WriteLiteral(writer, draft.Result, draft.PointsMin);
                writer.WritePropertyName("pointsMax");
                WriteLiteral(writer, draft.Result, draft.PointsMax);
                if (draft.Clamp)
                    writer.WriteBoolean("clamp", true);

                return true;
            case AlgorithmKind.Composite:
                writer.WriteString("combiner", draft.Combiner.ToString());
                if (!string.IsNullOrWhiteSpace(draft.Formula))
                    writer.WriteString("formula", draft.Formula);

                writer.WriteStartArray("children");
                foreach (var child in draft.Children)
                    WriteChild(writer, child);

                writer.WriteEndArray();
                return true;
            default:
                return false;
        }
    }

    private static void WriteChild(Utf8JsonWriter writer, AlgorithmChildDraft child)
    {
        writer.WriteStartObject();
        if (!string.IsNullOrWhiteSpace(child.Key))
            writer.WriteString("key", child.Key);

        if (!string.IsNullOrWhiteSpace(child.Ref))
            writer.WriteString("ref", child.Ref);

        if (child.Kind is not null) {
            writer.WriteString("kind", child.Kind.Value.ToString());
            writer.WritePropertyName("payload");
            var payload = child.Inline?.ToPayload();
            if (string.IsNullOrWhiteSpace(payload))
                writer.WriteStartObject();
            else
                using (var doc = JsonDocument.Parse(payload))
                    doc.RootElement.WriteTo(writer);

            if (string.IsNullOrWhiteSpace(payload))
                writer.WriteEndObject();
        }

        if (!string.IsNullOrWhiteSpace(child.Weight) && child.Weight.Trim() != "1") {
            writer.WritePropertyName("weight");
            WriteLiteral(writer, "double", child.Weight);
        }

        writer.WriteEndObject();
    }

    private static void WriteLiteral(Utf8JsonWriter writer, string alias, string? text)
    {
        var name = (alias ?? "double").Trim();
        var value = text ?? string.Empty;
        if (name.EndsWith("[]", StringComparison.Ordinal)) {
            writer.WriteStartArray();
            if (value.TrimStart().StartsWith('[')) {
                using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(value) ? "[]" : value);
                foreach (var item in document.RootElement.EnumerateArray())
                    item.WriteTo(writer);
            }

            writer.WriteEndArray();
            return;
        }

        if (name.Equals("bool", StringComparison.OrdinalIgnoreCase) && bool.TryParse(value, out var flag)) {
            writer.WriteBooleanValue(flag);
            return;
        }

        if (Numeric.Contains(name) && decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) {
            if (number == decimal.Truncate(number) && number is >= long.MinValue and <= long.MaxValue)
                writer.WriteNumberValue((long)number);
            else
                writer.WriteNumberValue(number);

            return;
        }

        writer.WriteStringValue(value);
    }

    private static List<AlgorithmTypeRow> ReadMap(JsonElement root, string name)
    {
        var rows = new List<AlgorithmTypeRow>();
        if (!root.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Object)
            return rows;

        foreach (var property in element.EnumerateObject())
            rows.Add(new() { Name = property.Name, Alias = property.Value.GetString() ?? "double" });

        return rows;
    }

    private static AlgorithmChildDraft ReadChild(JsonElement child)
    {
        var draft = new AlgorithmChildDraft();
        if (child.TryGetProperty("key", out var key))
            draft.Key = key.GetString();

        if (child.TryGetProperty("ref", out var reference))
            draft.Ref = reference.GetString();

        if (child.TryGetProperty("kind", out var kind) && kind.ValueKind == JsonValueKind.String && Enum.TryParse<AlgorithmKind>(kind.GetString(), true, out var parsed))
            draft.Kind = parsed;

        if (child.TryGetProperty("weight", out var weight))
            draft.Weight = TextOf(weight);

        if (child.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object) {
            draft.Inline = new() { Kind = draft.Kind ?? AlgorithmKind.DiscreteMap };
            Read(draft.Inline, payload);
        }

        return draft;
    }

    private static string TextOf(JsonElement element)
        => element.ValueKind switch {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            _ => element.GetRawText()
        };
}

/// <summary>JSON object for <c>POST Algorithms/preview</c>.</summary>
public static class AlgorithmContextRows
{
    /// <summary>Encodes each row with its alias. Rows whose names are not identifiers are skipped.</summary>
    public static string ToJson(IEnumerable<AlgorithmContextRow> rows)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) {
            writer.WriteStartObject();
            foreach (var row in rows) {
                if (!AlgorithmDraftRules.IsIdentifier(row.Name))
                    continue;

                writer.WritePropertyName(row.Name);
                WriteRow(writer, row);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteRow(Utf8JsonWriter writer, AlgorithmContextRow row)
    {
        var alias = string.IsNullOrWhiteSpace(row.Alias) ? "double" : row.Alias.Trim();
        if (alias.EndsWith("[]", StringComparison.Ordinal)) {
            writer.WriteStartArray();
            if (!string.IsNullOrWhiteSpace(row.Text) && row.Text.TrimStart().StartsWith('[')) {
                using var document = JsonDocument.Parse(row.Text);
                if (document.RootElement.ValueKind == JsonValueKind.Array) {
                    foreach (var item in document.RootElement.EnumerateArray())
                        item.WriteTo(writer);
                }
            }

            writer.WriteEndArray();
            return;
        }

        if (alias.Equals("bool", StringComparison.OrdinalIgnoreCase) && bool.TryParse(row.Text, out var flag)) {
            writer.WriteBooleanValue(flag);
            return;
        }

        if (AlgorithmDraftRules.IsNumericAlias(alias) && decimal.TryParse(row.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) {
            if (number == decimal.Truncate(number) && number is >= long.MinValue and <= long.MaxValue)
                writer.WriteNumberValue((long)number);
            else
                writer.WriteNumberValue(number);

            return;
        }

        writer.WriteStringValue(row.Text ?? string.Empty);
    }
}
