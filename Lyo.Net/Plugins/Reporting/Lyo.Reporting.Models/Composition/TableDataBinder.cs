using System.Text.Json;
using Lyo.Common.Core.Conversion;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Composition;

/// <summary>Maps dataset row objects onto <see cref="Table.Rows" /> using <see cref="Table.FieldMap" /> and column <c>Field</c> names. Does not execute Query or Sproc.</summary>
public static class TableDataBinder
{
    /// <summary>
    /// Replaces <paramref name="table" /> rows from <paramref name="rows" />. Missing parameter data yields an empty row list. Unknown columns become null cells.
    /// </summary>
    public static void Apply(Table table, IReadOnlyList<IReadOnlyDictionary<string, object?>>? rows)
    {
        ArgumentHelpers.ThrowIfNull(table);
        table.Rows.Clear();
        table.Rows.AddRange(ProjectRows(table, rows));
    }

    /// <summary>
    /// Builds table rows from dataset objects without mutating <paramref name="table" />. Used by the design canvas so example JSON can paint without cloning.
    /// </summary>
    public static List<TableRow> ProjectRows(Table table, IReadOnlyList<IReadOnlyDictionary<string, object?>>? rows)
    {
        ArgumentHelpers.ThrowIfNull(table);
        var result = new List<TableRow>();
        if (rows is null || rows.Count == 0)
            return result;

        foreach (var row in rows) {
            var cells = new List<object?>(table.Columns.Count);
            foreach (var column in table.Columns) {
                var source = ResolveSourceField(table, column);
                cells.Add(string.IsNullOrEmpty(source) ? null : Lookup(row, source));
            }

            result.Add(new() { Cells = cells });
        }

        return result;
    }

    /// <summary>Parses a JSON array of objects into row dictionaries. Non-array or invalid JSON returns an empty list.</summary>
    public static IReadOnlyList<Dictionary<string, object?>> ParseRows(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var rows = new List<Dictionary<string, object?>>();
            foreach (var el in doc.RootElement.EnumerateArray()) {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;

                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in el.EnumerateObject())
                    dict[prop.Name] = TypeConversion.FromJsonElement(prop.Value);

                rows.Add(dict);
            }

            return rows;
        }
        catch (JsonException) {
            return [];
        }
    }

    private static string? ResolveSourceField(Table table, TableColumn column)
    {
        if (!string.IsNullOrWhiteSpace(column.Field) && table.FieldMap.TryGetValue(column.Field, out var mapped) && !string.IsNullOrWhiteSpace(mapped))
            return mapped;

        if (!string.IsNullOrWhiteSpace(column.Header) && table.FieldMap.TryGetValue(column.Header, out mapped) && !string.IsNullOrWhiteSpace(mapped))
            return mapped;

        return string.IsNullOrWhiteSpace(column.Field) ? column.Header : column.Field;
    }

    private static object? Lookup(IReadOnlyDictionary<string, object?> row, string name)
    {
        if (row.TryGetValue(name, out var value))
            return value;

        foreach (var kvp in row) {
            if (string.Equals(kvp.Key, name, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return null;
    }
}
