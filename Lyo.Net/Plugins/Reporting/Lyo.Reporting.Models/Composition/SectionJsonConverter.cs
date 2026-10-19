using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Composition;

/// <summary>
/// Reads <see cref="Section" /> from the <c>Controls</c> shape and writes <c>Controls</c> and <c>Subsections</c>.
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

        if (TryGetProperty(root, "controls", out var controlsEl) && controlsEl.ValueKind == JsonValueKind.Array && controlsEl.GetArrayLength() > 0)
            section.Controls = DeserializeControls(controlsEl, options);

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
