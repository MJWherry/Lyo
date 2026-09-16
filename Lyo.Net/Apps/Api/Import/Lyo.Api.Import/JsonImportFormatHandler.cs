using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Lyo.Api.Models.Common.Request;
using Lyo.Exceptions;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Enums;
using Lyo.Json;

namespace Lyo.Api.Import;

/// <summary>Built-in JSON import. Other formats convert to JSON first. An empty map binds property names. A field map renames sources onto targets.</summary>
public sealed class JsonImportFormatHandler : IImportFormatHandler
{
    private readonly IJsonService _json;

    /// <summary>Uses <see cref="JsonService.Default" /> when <paramref name="json" /> is omitted.</summary>
    public JsonImportFormatHandler(IJsonService? json = null) => _json = json ?? JsonService.Default;

    /// <inheritdoc />
    public ImportFormat Format => ImportFormat.Json;

    /// <inheritdoc />
    public ImportJsonConversion Convert(ImportRequest request)
    {
        ArgumentHelpers.ThrowIfNull(request);
        var mode = request.JsonMode;
        if (mode == ImportJsonMode.Array && !string.IsNullOrWhiteSpace(request.RowsPath))
            mode = ImportJsonMode.Wrapped;

        return ImportJsonConversion.Ok(request.Content ?? string.Empty, mode, request.RowsPath);
    }

    /// <summary>Binds JSON rows. <see cref="ImportRequest.Fields" /> renames sources. An empty map matches property names, ignoring case.</summary>
    public ImportResult<T> Read<T>(ImportRequest request)
        where T : new()
    {
        var content = request.Content ?? string.Empty;
        var parsed = request.JsonMode switch {
            ImportJsonMode.Ndjson => _json.ParseNdjsonAsResult<JsonElement>(content),
            ImportJsonMode.Wrapped => _json.ParseArrayAsResult<JsonElement>(content, request.RowsPath),
            _ => _json.ParseArrayAsResult<JsonElement>(content)
        };

        if (!parsed.IsSuccess)
            return ImportResult<T>.Fail(parsed.Message ?? "JSON document is not valid.");

        var map = FieldMap(request.Fields);
        var properties = WritableProperties<T>();
        var rows = new List<T>();
        var issues = ToIssues(parsed.Issues);
        for (var index = 0; index < parsed.Rows.Count; index++) {
            var element = parsed.Rows[index];
            if (element.ValueKind != JsonValueKind.Object) {
                issues.Add(new() { Index = index, Message = "JSON element is not an object.", Raw = element.GetRawText() });
                continue;
            }

            if (!TryBind(element, index, map, properties, out T row, out var issue)) {
                issues.Add(issue);
                continue;
            }

            rows.Add(row);
        }

        return ImportResult<T>.Ok(rows, issues);
    }

    private static Dictionary<string, ImportFieldMap>? FieldMap(IReadOnlyList<ImportFieldMap>? fields)
    {
        if (fields is null || fields.Count == 0)
            return null;

        var map = new Dictionary<string, ImportFieldMap>(fields.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields) {
            if (!string.IsNullOrWhiteSpace(field.Source))
                map[field.Source] = field;
        }

        return map.Count == 0 ? null : map;
    }

    private static Dictionary<string, PropertyInfo> WritableProperties<T>()
    {
        var properties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.CanWrite && !properties.ContainsKey(property.Name))
                properties.Add(property.Name, property);
        }

        return properties;
    }

    private static bool TryBind<T>(
        JsonElement element,
        int index,
        Dictionary<string, ImportFieldMap>? map,
        Dictionary<string, PropertyInfo> properties,
        out T row,
        out ImportIssue issue)
        where T : new()
    {
        row = new();
        if (map is null) {
            foreach (var property in properties.Values) {
                if (!TryFind(element, property.Name, out var value) || value.ValueKind == JsonValueKind.Null)
                    continue;

                if (!TryAssign(property, row, property.Name, value, index, out issue))
                    return false;
            }

            issue = null!;
            return true;
        }

        foreach (var field in map.Values) {
            if (!properties.TryGetValue(field.Target, out var property))
                continue;

            if (!TryFind(element, field.Source, out var value) || value.ValueKind == JsonValueKind.Null) {
                if (!field.Required)
                    continue;

                issue = new() { Index = index, Field = field.Source, Message = $"Required field '{field.Source}' is missing.", Raw = element.GetRawText() };
                return false;
            }

            if (!TryAssign(property, row, field.Source, value, index, out issue))
                return false;
        }

        issue = null!;
        return true;
    }

    private static bool TryFind(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject()) {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            value = property.Value;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryAssign<T>(PropertyInfo property, T target, string field, JsonElement value, int index, out ImportIssue issue)
        where T : new()
    {
        try {
            Assign(property, target!, value);
            issue = null!;
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException or JsonException) {
            issue = new() { Index = index, Field = field, Message = ex.Message, Raw = value.GetRawText() };
            return false;
        }
    }

    private static void Assign(PropertyInfo property, object target, JsonElement element)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type == typeof(string)) {
            property.SetValue(target, element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString());
            return;
        }

        var text = element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        property.SetValue(target, System.Convert.ChangeType(text, type, CultureInfo.InvariantCulture));
    }

    private static List<ImportIssue> ToIssues(IReadOnlyList<JsonImportIssue> issues)
    {
        var copy = new List<ImportIssue>(issues.Count);
        foreach (var issue in issues)
            copy.Add(new() { Index = issue.Index, Field = issue.Field, Message = issue.Message, Raw = issue.Raw });

        return copy;
    }
}
