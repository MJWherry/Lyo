using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Common.Core.Enums;
using Lyo.Json;
using Lyo.Result;
using Tomlyn;
using Tomlyn.Model;

namespace Lyo.Toml;

internal sealed class TomlEngine
{
    private readonly JsonService _json;
    private readonly string _items;
    private readonly bool _inline;
    private readonly TomlDateStyle _dateStyle;
    private readonly bool _unix;
    private readonly global::Tomlyn.TomlSerializerOptions _serializer;
    private readonly MemberOrder _order;
    private readonly string[] _orderNames;

    public TomlEngine(TomlOptions options)
    {
        _items = string.IsNullOrEmpty(options.ItemsKey) ? "items" : options.ItemsKey;
        _inline = options.NestedTables == TomlNestedTables.Inline;
        _dateStyle = options.DateStyle;
        _unix = options.DateTimeStyle == DocumentDateTimeStyle.UnixMilliseconds;
        Encoding = options.Encoding;
        _order = options.Order;
        _orderNames = options.OrderNames.ToArray();
        _serializer = new() { WriteIndented = options.WriteIndented };
        _json = new(Copy(options));
    }

    public Encoding Encoding { get; }

    public T Parse<T>(string toml)
    {
        var node = ToNode(toml);
        if (IsList(typeof(T)) && node is JsonObject obj && obj[_items] is JsonArray items)
            node = items;
        return node is null ? default! : _json.ParseString<T>(node.ToJsonString());
    }

    public string Export<T>(T value) => FromNode(JsonNode.Parse(_json.ExportToJsonString(value)));

    public string ToJson(string toml)
    {
        var node = ToNode(toml);
        return node is null ? "null" : node.ToJsonString();
    }

    public string FromJson(string json) => FromNode(JsonNode.Parse(json));

    public JsonNode? ToNode(string toml)
    {
        if (string.IsNullOrWhiteSpace(toml))
            return null;
        var table = global::Tomlyn.TomlSerializer.Deserialize<TomlTable>(toml);
        return JsonNodeGraph.FromClr(table);
    }

    private string FromNode(JsonNode? node) => FromNode(node, applyOrder: true);

    public string ExportPreservingKeys<T>(T value) => FromNode(JsonNode.Parse(_json.ExportPreservingKeyOrder(value)), applyOrder: false);

    /// <summary>Writes a data-table row array as <c>[[items]]</c> under <see cref="TomlOptions.ItemsKey" />.</summary>
    public string ExportRows(JsonArray rows) => FromNode(rows, applyOrder: false);

    /// <summary>Reads the items array of tables into a data table. Cell text stays text.</summary>
    public Result<DataTable.Models.DataTable> ParseRows(string toml, bool hasFooterRow)
    {
        var node = ToNode(toml);
        if (node is JsonObject obj && obj[_items] is JsonArray items)
            return DataTableDocument.FromRows(items, hasFooterRow);
        return Result<DataTable.Models.DataTable>.Failure("Expected an array of tables.", "InvalidDataTable");
    }

    private string FromNode(JsonNode? node, bool applyOrder)
    {
        if (applyOrder)
            node = JsonMemberOrder.Apply(node, _order, _orderNames);
        if (node is JsonArray array)
            node = new JsonObject { [_items] = array };
        var model = ToToml(node, nested: false);
        return model is null ? "" : global::Tomlyn.TomlSerializer.Serialize(model, _serializer);
    }

    private object? ToToml(JsonNode? node, bool nested)
    {
        switch (node) {
            case null:
                return null;
            case JsonObject obj: {
                var table = new TomlTable(inline: nested && _inline);
                foreach (var property in obj) {
                    var child = property.Value is JsonArray array ? ToArray(array) : ToToml(property.Value, nested: true);
                    if (child is not null)
                        table[property.Key] = child;
                }

                return table;
            }
            case JsonArray array:
                return ToArray(array);
            case JsonValue value:
                return Scalar(value);
            default:
                return null;
        }
    }

    private object ToArray(JsonArray array)
    {
        var tables = true;
        foreach (var item in array) {
            if (item is not null && item is not JsonObject)
                tables = false;
        }

        if (tables && array.Count > 0) {
            var tableArray = new TomlTableArray();
            foreach (var item in array) {
                if (item is JsonObject obj)
                    tableArray.Add((TomlTable)ToToml(obj, nested: false)!);
            }

            return tableArray;
        }

        var list = new TomlArray();
        foreach (var item in array) {
            if (item is null)
                continue;
            var child = ToToml(item, nested: true);
            if (child is not null)
                list.Add(child);
        }

        return list;
    }

    private object? Scalar(JsonValue value)
    {
        if (!_unix && value.GetValueKind() == JsonValueKind.String && TryDate(value.GetValue<string>(), out var date))
            return date;
        return JsonNodeGraph.ToClr(value);
    }

    private bool TryDate(string text, out TomlDateTime date)
    {
        date = default;
        if (text.Length < 8 || !char.IsDigit(text[0]))
            return false;
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            return false;
        date = _dateStyle switch {
            TomlDateStyle.Local => new(DateTime.SpecifyKind(dto.DateTime, DateTimeKind.Unspecified)),
            TomlDateStyle.LocalDate => new(dto.Year, dto.Month, dto.Day),
            _ => new(dto, 0, dto.Offset == TimeSpan.Zero ? global::Tomlyn.TomlDateTimeKind.OffsetDateTimeByZ : global::Tomlyn.TomlDateTimeKind.OffsetDateTimeByNumber)
        };
        return true;
    }

    private static bool IsList(Type type)
    {
        if (type == typeof(string))
            return false;
        if (type.IsArray)
            return true;
        foreach (var iface in type.GetInterfaces()) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return true;
        }

        return false;
    }

    private static Action<JsonOptions> Copy(TomlOptions options)
        => target => {
            target.Naming = options.Naming;
            target.Enums = options.Enums;
            target.ReadNumbersFromStrings = options.ReadNumbersFromStrings;
            target.WriteNumbersAsStrings = options.WriteNumbersAsStrings;
            target.UseStringScalarConverters = options.UseStringScalarConverters;
            target.IgnoreNulls = options.IgnoreNulls;
            target.IgnoreDefaults = options.IgnoreDefaults;
            target.IgnoreEmptyStrings = options.IgnoreEmptyStrings;
            target.IgnoreEmptyCollections = options.IgnoreEmptyCollections;
            target.EmptyStringAsNull = options.EmptyStringAsNull;
            target.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
            target.UnmappedMembers = options.UnmappedMembers;
            target.DateTimeStyle = options.DateTimeStyle;
            target.Culture = options.Culture;
            target.MaxDepth = options.MaxDepth;
            target.WriteIndented = false;
            target.AllowTrailingCommas = options.AllowTrailingCommas;
            target.CommentHandling = options.CommentHandling;
            target.Encoding = options.Encoding;
            target.Order = options.Order;
            target.OrderNames.AddRange(options.OrderNames);
        };
}
