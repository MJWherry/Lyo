using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Composition;

/// <summary>
/// Reads <see cref="Section" /> from either the current <c>Controls</c> shape or legacy <c>Columns</c> / <c>ContentBlocks</c> / table-shaped <c>Grids</c>.
/// Writes only <c>Controls</c> and <c>Subsections</c>.
/// </summary>
public sealed class SectionJsonConverter : JsonConverter<Section>
{
    public override Section Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var section = new Section();
        AssignScalars(root, section, options);
        if (root.TryGetProperty("subsections", out var subs) || root.TryGetProperty("Subsections", out subs))
            section.Subsections = DeserializeList<Section>(subs, options);

        if (TryGetProperty(root, "controls", out var controlsEl) && controlsEl.ValueKind == JsonValueKind.Array && controlsEl.GetArrayLength() > 0) {
            section.Controls = DeserializeControls(controlsEl, options);
            return section;
        }

        MigrateLegacy(root, section, options);
        return section;
    }

    public override void Write(Utf8JsonWriter writer, Section value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        WriteString(writer, "id", value.Id);
        WriteString(writer, "title", value.Title);
        WriteString(writer, "subtitle", value.Subtitle);
        WriteString(writer, "description", value.Description);
        if (value.Order != 0)
            writer.WriteNumber("order", value.Order);

        WriteString(writer, "visibleWhen", value.VisibleWhen);
        if (value.Collapsed)
            writer.WriteBoolean("collapsed", true);

        writer.WritePropertyName("controls");
        JsonSerializer.Serialize(writer, value.Controls, options);
        writer.WritePropertyName("subsections");
        JsonSerializer.Serialize(writer, value.Subsections, options);
        if (value.Styles.Count > 0) {
            writer.WritePropertyName("styles");
            JsonSerializer.Serialize(writer, value.Styles, options);
        }

        writer.WriteEndObject();
    }

    private static void AssignScalars(JsonElement root, Section section, JsonSerializerOptions options)
    {
        if (TryGetString(root, "id", out var id))
            section.Id = id;
        if (TryGetString(root, "title", out var title))
            section.Title = title;
        if (TryGetString(root, "subtitle", out var subtitle))
            section.Subtitle = subtitle;
        if (TryGetString(root, "description", out var description))
            section.Description = description;
        if (TryGetInt(root, "order", out var order))
            section.Order = order;
        if (TryGetString(root, "visibleWhen", out var visibleWhen))
            section.VisibleWhen = visibleWhen;
        if (TryGetBool(root, "collapsed", out var collapsed))
            section.Collapsed = collapsed;
        if (TryGetProperty(root, "styles", out var styles) && styles.ValueKind == JsonValueKind.Object)
            section.Styles = JsonSerializer.Deserialize<Dictionary<string, string>>(styles.GetRawText(), options) ?? [];
    }

    private static void MigrateLegacy(JsonElement root, Section section, JsonSerializerOptions options)
    {
        var cards = TryGetProperty(root, "columns", out var columnsEl) && columnsEl.ValueKind == JsonValueKind.Array
            ? DeserializeList<Card>(columnsEl, options)
            : [];
        var blocks = TryGetProperty(root, "contentBlocks", out var blocksEl) && blocksEl.ValueKind == JsonValueKind.Array
            ? DeserializeList<Block>(blocksEl, options)
            : [];
        var tables = new List<Table>();
        var grids = new List<Grid>();
        if (TryGetProperty(root, "grids", out var gridsEl) && gridsEl.ValueKind == JsonValueKind.Array) {
            foreach (var item in gridsEl.EnumerateArray()) {
                if (LooksLikeTable(item))
                    tables.Add(JsonSerializer.Deserialize<Table>(item.GetRawText(), options) ?? new Table());
                else {
                    var grid = JsonSerializer.Deserialize<Grid>(item.GetRawText(), options) ?? new Grid();
                    grid.Controls = [..grid.Controls.Where(c => c is not Grid)];
                    grids.Add(grid);
                }
            }
        }

        var bandOrder = TryGetInt(root, "columnBandOrder", out var bo) ? bo : 0;
        var allZero = bandOrder == 0 && blocks.TrueForAll(b => b.Order == 0) && tables.TrueForAll(t => t.Order == 0) && grids.TrueForAll(g => g.Order == 0);
        if (allZero) {
            if (cards.Count > 0)
                section.Controls.Add(WrapCards(cards));

            foreach (var block in blocks)
                section.Controls.Add(block);

            foreach (var table in tables)
                section.Controls.Add(table);

            foreach (var grid in grids)
                section.Controls.Add(grid);

            return;
        }

        var mixed = new List<(int Order, int Index, Control Control)>();
        var index = 0;
        if (cards.Count > 0)
            mixed.Add((bandOrder, index++, WrapCards(cards)));

        foreach (var block in blocks)
            mixed.Add((block.Order, index++, block));

        foreach (var table in tables)
            mixed.Add((table.Order, index++, table));

        foreach (var grid in grids)
            mixed.Add((grid.Order, index++, grid));

        foreach (var item in mixed.OrderBy(x => x.Order).ThenBy(x => x.Index))
            section.Controls.Add(item.Control);
    }

    private static Grid WrapCards(List<Card> cards)
        => new() {
            ColumnCount = Math.Max(cards.Count, 1),
            TemplateColumns = "repeat(auto-fit, minmax(220px, 1fr))",
            Controls = [..cards.Cast<Control>()]
        };

    private static bool LooksLikeTable(JsonElement el)
    {
        if (TryGetString(el, "kind", out var kind) && string.Equals(kind, "table", StringComparison.OrdinalIgnoreCase))
            return true;

        if (Has(el, "showHeaders") || Has(el, "striped") || Has(el, "bordered") || Has(el, "showRowNumbers") || Has(el, "fieldMap") || Has(el, "dataParameterKey"))
            return true;

        if (TryGetProperty(el, "columns", out var columns) && columns.ValueKind == JsonValueKind.Array && columns.GetArrayLength() > 0) {
            var first = columns[0];
            if (Has(first, "header") || Has(first, "field"))
                return true;
        }

        if (TryGetProperty(el, "rows", out var rows) && rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() > 0) {
            var first = rows[0];
            if (Has(first, "cells"))
                return true;
        }

        return false;
    }

    private static List<Control> DeserializeControls(JsonElement array, JsonSerializerOptions options)
    {
        var list = new List<Control>();
        foreach (var item in array.EnumerateArray()) {
            var control = JsonSerializer.Deserialize<Control>(item.GetRawText(), options);
            if (control is null)
                continue;

            if (control is Grid grid)
                grid.Controls = [..grid.Controls.Where(c => c is not Grid)];

            list.Add(control);
        }

        return list;
    }

    private static List<T> DeserializeList<T>(JsonElement array, JsonSerializerOptions options)
    {
        var list = new List<T>();
        foreach (var item in array.EnumerateArray()) {
            var value = JsonSerializer.Deserialize<T>(item.GetRawText(), options);
            if (value is not null)
                list.Add(value);
        }

        return list;
    }

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            writer.WriteString(name, value);
    }

    private static bool TryGetProperty(JsonElement el, string camel, out JsonElement value)
    {
        if (el.TryGetProperty(camel, out value))
            return true;

        var pascal = char.ToUpperInvariant(camel[0]) + camel[1..];
        return el.TryGetProperty(pascal, out value);
    }

    private static bool Has(JsonElement el, string camel) => TryGetProperty(el, camel, out _);

    private static bool TryGetString(JsonElement el, string camel, out string value)
    {
        value = string.Empty;
        if (!TryGetProperty(el, camel, out var prop) || prop.ValueKind != JsonValueKind.String)
            return false;

        value = prop.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetInt(JsonElement el, string camel, out int value)
    {
        value = 0;
        if (!TryGetProperty(el, camel, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out value))
            return true;

        return prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out value);
    }

    private static bool TryGetBool(JsonElement el, string camel, out bool value)
    {
        value = false;
        if (!TryGetProperty(el, camel, out var prop))
            return false;

        if (prop.ValueKind is JsonValueKind.True or JsonValueKind.False) {
            value = prop.GetBoolean();
            return true;
        }

        return prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out value);
    }
}
