using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Exceptions;
using Lyo.Result;

namespace Lyo.Json;

/// <summary>Projects a <see cref="DataTable.Models.DataTable" /> to a JSON array of string-valued objects, and back. Naming, member order, and empty-string omission do not apply.</summary>
internal static class DataTableDocument
{
    /// <summary>Writes one object per body row. A footer with cells is the last object. A blank header becomes <c>Column{index}</c>. A repeated header throws.</summary>
    public static JsonArray ToRows(DataTable.Models.DataTable table)
    {
        ArgumentHelpers.ThrowIfNull(table);
        var width = table.MaxColumn >= 0 ? table.MaxColumn + 1 : 0;
        var names = Headers(table, width);
        var rows = new JsonArray();
        foreach (var row in table.Rows)
            rows.Add(Object(names, column => Text(row.Cells, column)));
        if (table.Footer.Count > 0)
            rows.Add(Object(names, column => Text(table.Footer, column)));
        return rows;
    }

    /// <summary>
    /// Reads an array of objects. <paramref name="hasFooterRow" /> moves the last object into the footer. A root that is not an array of objects, or a nested cell, fails the
    /// result.
    /// </summary>
    public static Result<DataTable.Models.DataTable> FromRows(JsonNode? root, bool hasFooterRow)
    {
        if (root is not JsonArray array)
            return Result<DataTable.Models.DataTable>.Failure("Expected an array of objects.", "InvalidDataTable");

        var objects = new List<JsonObject>(array.Count);
        foreach (var item in array) {
            if (item is not JsonObject obj)
                return Result<DataTable.Models.DataTable>.Failure("Expected an array of objects.", "InvalidDataTable");
            foreach (var property in obj)
                if (property.Value is JsonObject or JsonArray)
                    return Result<DataTable.Models.DataTable>.Failure("A cell must be a scalar.", "InvalidDataTable");
            objects.Add(obj);
        }

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var obj in objects)
            foreach (var property in obj)
                if (seen.Add(property.Key))
                    names.Add(property.Key);

        var bodyCount = objects.Count;
        JsonObject? footer = null;
        if (hasFooterRow && bodyCount > 0) {
            footer = objects[^1];
            bodyCount--;
        }

        var table = new DataTable.Models.DataTable();
        for (var column = 0; column < names.Count; column++)
            table.SetHeader(column, names[column]);
        for (var row = 0; row < bodyCount; row++)
            Write(table, names, objects[row], row, footer: false);
        if (footer is not null)
            Write(table, names, footer, 0, footer: true);
        return table;
    }

    private static string[] Headers(DataTable.Models.DataTable table, int width)
    {
        var names = new string[width];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var column = 0; column < width; column++) {
            var raw = table.Headers.TryGetValue(column, out var cell) ? cell.DisplayValue : "";
            var name = string.IsNullOrWhiteSpace(raw) ? "Column" + column.ToString(CultureInfo.InvariantCulture) : raw;
            if (!seen.Add(name))
                throw new ArgumentException($"Header '{name}' is repeated.", nameof(table));
            names[column] = name;
        }

        return names;
    }

    private static JsonObject Object(string[] names, Func<int, string> value)
    {
        var obj = new JsonObject();
        for (var column = 0; column < names.Length; column++)
            obj[names[column]] = JsonValue.Create(value(column));
        return obj;
    }

    private static string Text(IReadOnlyDictionary<int, DataTable.Models.IDataTableCell> cells, int column)
        => cells.TryGetValue(column, out var cell) ? cell.DisplayValue ?? "" : "";

    private static void Write(DataTable.Models.DataTable table, List<string> names, JsonObject row, int index, bool footer)
    {
        for (var column = 0; column < names.Count; column++) {
            var text = row.TryGetPropertyValue(names[column], out var node) ? Scalar(node) : "";
            if (footer)
                table.SetFooter(column, text);
            else
                table.SetCell(index, column, text);
        }
    }

    private static string Scalar(JsonNode? node)
    {
        if (node is not JsonValue value)
            return "";
        return value.GetValueKind() switch {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.Null => "",
            _ => value.ToJsonString()
        };
    }
}
