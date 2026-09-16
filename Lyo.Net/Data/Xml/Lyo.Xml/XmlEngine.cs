using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;
using Lyo.Common.Core.Enums;
using Lyo.Json;
using Lyo.Result;

namespace Lyo.Xml;

internal sealed class XmlEngine
{
    private readonly JsonService _json;
    private readonly string _root;
    private readonly string _item;
    private readonly bool _omitDeclaration;
    private readonly XNamespace _namespace;
    private readonly bool _attributes;
    private readonly bool _omitEmpty;
    private readonly bool _indented;
    private readonly bool _readNumbers;
    private readonly bool _emptyAsNull;
    private readonly MemberOrder _order;
    private readonly string[] _orderNames;
    private readonly JsonNamingPolicy? _naming;
    private readonly Dictionary<(Type Type, string Name), bool?> _typed;
    private readonly Dictionary<string, bool?> _named;
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

    public XmlEngine(XmlOptions options)
    {
        _root = options.RootElementName;
        _item = string.IsNullOrEmpty(options.ItemElementName) ? "item" : options.ItemElementName;
        _omitDeclaration = options.OmitXmlDeclaration;
        _namespace = string.IsNullOrEmpty(options.Namespace) ? XNamespace.None : XNamespace.Get(options.Namespace);
        _attributes = options.ScalarsAsAttributes;
        _omitEmpty = options.OmitEmptyElements;
        _indented = options.WriteIndented;
        _readNumbers = options.ReadNumbersFromStrings;
        _emptyAsNull = options.EmptyStringAsNull;
        _order = options.Order;
        _orderNames = [.. options.OrderNames];
        _naming = NamingPolicy(options.Naming);
        _typed = new(options.TypedMembers.Count);
        foreach (var member in options.TypedMembers)
            _typed[member.Key] = member.Value;
        _named = new(StringComparer.Ordinal);
        foreach (var member in options.NamedMembers)
            _named[member.Key] = member.Value;
        Encoding = options.Encoding;
        _json = new(Copy(options));
    }

    public Encoding Encoding { get; }

    public T Parse<T>(string xml)
    {
        var node = ToNode(xml);
        return node is null ? default! : _json.ParseString<T>(node.ToJsonString());
    }

    public string Export<T>(T value) => FromNode(JsonNode.Parse(_json.ExportToJsonString(value)), typeof(T));

    public string ToJson(string xml)
    {
        var node = ToNode(xml);
        return node is null ? "null" : node.ToJsonString();
    }

    public string FromJson(string json) => FromNode(JsonNode.Parse(json), null);

    public JsonNode? ToNode(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;
        var root = XDocument.Parse(xml).Root;
        return root is null ? null : Read(root);
    }

    private string FromNode(JsonNode? node, Type? declaredType) => FromNode(node, declaredType, applyOrder: true);

    public string ExportPreservingKeys<T>(T value) => FromNode(JsonNode.Parse(_json.ExportPreservingKeyOrder(value)), null, applyOrder: false);

    /// <summary>Writes a data-table row array as item elements. Cell text is element text, including empty strings and leading zeros.</summary>
    public string ExportRows(JsonArray rows)
    {
        var root = new XElement(Name(string.IsNullOrEmpty(_root) ? "items" : _root));
        foreach (var row in rows) {
            var item = new XElement(Name(_item));
            if (row is JsonObject obj) {
                foreach (var property in obj)
                    item.Add(new XElement(Name(property.Key)) { Value = CellText(property.Value) });
            }

            root.Add(item);
        }

        var save = _indented ? SaveOptions.None : SaveOptions.DisableFormatting;
        var body = new XDocument(root).ToString(save);
        if (_omitDeclaration)
            return body;
        return $"<?xml version=\"1.0\" encoding=\"{Encoding.WebName}\"?>\n{body}";
    }

    /// <summary>Reads item elements into a data table. Element text stays text. Encoded local names are decoded.</summary>
    public Result<DataTable.Models.DataTable> ParseRows(string xml, bool hasFooterRow)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return Result<DataTable.Models.DataTable>.Failure("Expected an array of objects.", "InvalidDataTable");
        var root = XDocument.Parse(xml).Root;
        if (root is null)
            return Result<DataTable.Models.DataTable>.Failure("Expected an array of objects.", "InvalidDataTable");
        var itemName = LocalName(_item);
        var rows = new JsonArray();
        foreach (var item in root.Elements()) {
            if (item.Name.LocalName != itemName)
                continue;
            var obj = new JsonObject();
            foreach (var child in item.Elements())
                obj[XmlConvert.DecodeName(child.Name.LocalName)] = JsonValue.Create(child.Value);
            rows.Add(obj);
        }

        return DataTableDocument.FromRows(rows, hasFooterRow);
    }

    private static string CellText(JsonNode? node)
    {
        if (node is JsonValue value && value.GetValueKind() == JsonValueKind.String)
            return value.GetValue<string>();
        return node is null ? "" : node.ToString() ?? "";
    }

    private string FromNode(JsonNode? node, Type? declaredType, bool applyOrder)
    {
        if (applyOrder)
            node = JsonMemberOrder.Apply(node, _order, _orderNames);
        var root = new XElement(Name(RootName(declaredType, node)));
        if (node is JsonArray array)
            WriteArray(root, array, _item, ElementType(declaredType));
        else if (node is JsonObject obj)
            WriteObject(root, obj, ObjectType(declaredType));
        else if (node is JsonValue value)
            root.Value = ScalarText(value);

        var save = _indented ? SaveOptions.None : SaveOptions.DisableFormatting;
        var body = new XDocument(root).ToString(save);
        if (_omitDeclaration)
            return body;
        return $"<?xml version=\"1.0\" encoding=\"{Encoding.WebName}\"?>\n{body}";
    }

    private void WriteObject(XElement parent, JsonObject obj, Type? type)
    {
        foreach (var property in obj) {
            if (property.Value is JsonArray array) {
                WriteArray(parent, array, property.Key, ElementType(ChildType(type, property.Key)));
                continue;
            }

            if (property.Value is null) {
                if (!_omitEmpty)
                    parent.Add(new XElement(Name(property.Key)));
                continue;
            }

            if (property.Value is JsonValue scalar) {
                var text = ScalarText(scalar);
                if (_omitEmpty && text.Length == 0)
                    continue;
                if (AsAttribute(type, property.Key)) {
                    parent.SetAttributeValue(property.Key, text);
                    continue;
                }

                parent.Add(new XElement(Name(property.Key)) { Value = text });
                continue;
            }

            var child = new XElement(Name(property.Key));
            WriteObject(child, (JsonObject)property.Value, ObjectType(ChildType(type, property.Key)));
            parent.Add(child);
        }
    }

    private void WriteArray(XElement parent, JsonArray array, string itemName, Type? itemType)
    {
        foreach (var item in array) {
            if (item is null)
                continue;
            var element = new XElement(Name(itemName));
            if (item is JsonObject obj)
                WriteObject(element, obj, ObjectType(itemType));
            else if (item is JsonArray nested)
                WriteArray(element, nested, _item, ElementType(itemType));
            else if (item is JsonValue value)
                element.Value = ScalarText(value);
            parent.Add(element);
        }
    }

    private bool AsAttribute(Type? type, string key)
    {
        var choice = Choice(type, key);
        if (choice is false)
            return false;
        if (choice is true)
            return true;
        return _attributes;
    }

    private bool? Choice(Type? type, string key)
    {
        var property = type is null ? null : FindProperty(type, key);
        if (property is not null && _typed.TryGetValue((type!, property.Name), out var typed))
            return typed;

        var mark = property?.GetCustomAttribute<XmlMemberAttribute>();
        if (mark is not null)
            return mark.AsAttribute;

        return _named.TryGetValue(key, out var named) ? named : null;
    }

    private PropertyInfo? FindProperty(Type type, string key)
    {
        var properties = _properties.GetOrAdd(type, static declared => declared.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0 && property.CanRead).ToArray());
        foreach (var property in properties) {
            if (SerializedName(property) == key)
                return property;
        }

        return null;
    }

    private Type? ChildType(Type? type, string key)
    {
        var property = FindProperty(type ?? typeof(object), key);
        return property is null ? null : Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
    }

    private string SerializedName(PropertyInfo property)
    {
        var named = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (named is not null && !string.IsNullOrEmpty(named.Name))
            return named.Name;

        return _naming?.ConvertName(property.Name) ?? property.Name;
    }

    private static Type? ObjectType(Type? type)
    {
        type = type is null ? null : Nullable.GetUnderlyingType(type) ?? type;
        if (type is null || type == typeof(object) || type == typeof(JsonNode) || type == typeof(JsonObject) || type == typeof(JsonArray) || type == typeof(string) || IsList(type))
            return null;

        return type;
    }

    private static Type? ElementType(Type? type)
    {
        if (type is null)
            return null;

        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string))
            return null;
        if (type.IsArray)
            return type.GetElementType();

        foreach (var iface in type.GetInterfaces()) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

    private static JsonNamingPolicy? NamingPolicy(DocumentNaming naming)
        => naming switch {
            DocumentNaming.PascalCase => null,
            DocumentNaming.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            DocumentNaming.KebabCase => JsonNamingPolicy.KebabCaseLower,
            _ => JsonNamingPolicy.CamelCase
        };

    private JsonNode? Read(XElement element)
    {
        var children = element.Elements().ToList();
        var attributes = element.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration).ToList();
        if (children.Count == 0 && attributes.Count == 0)
            return Leaf(element.Value);

        if (children.Count == 0) {
            var only = new JsonObject();
            foreach (var attribute in attributes)
                only[attribute.Name.LocalName] = Leaf(attribute.Value);
            return only;
        }

        var names = children.Select(child => child.Name.LocalName).Distinct(StringComparer.Ordinal).ToList();
        if (names.Count == 1 && attributes.Count == 0 && (children.Count > 1 || names[0] == _item))
            return new JsonArray(children.Select(Read).ToArray());

        var obj = new JsonObject();
        foreach (var attribute in attributes)
            obj[attribute.Name.LocalName] = Leaf(attribute.Value);
        foreach (var group in children.GroupBy(child => child.Name.LocalName, StringComparer.Ordinal)) {
            if (group.Skip(1).Any())
                obj[group.Key] = new JsonArray(group.Select(Read).ToArray());
            else
                obj[group.Key] = Read(group.First());
        }

        return obj;
    }

    private JsonNode? Leaf(string text)
    {
        if (text.Length == 0)
            return _emptyAsNull ? null : JsonValue.Create(string.Empty);
        if (!_readNumbers)
            return JsonValue.Create(text);
        return JsonNodeGraph.FromClr(Coerce(text));
    }

    private XName Name(string local) => _namespace + LocalName(local);

    private static string LocalName(string local)
    {
        if (string.IsNullOrEmpty(local))
            return "field";
        try {
            XmlConvert.VerifyNCName(local);
            return local;
        }
        catch (Exception ex) when (ex is XmlException or ArgumentException) {
            var encoded = XmlConvert.EncodeLocalName(local);
            return string.IsNullOrEmpty(encoded) ? "field" : encoded;
        }
    }

    private string RootName(Type? declaredType, JsonNode? node)
    {
        if (!string.IsNullOrEmpty(_root))
            return _root;
        if (node is JsonArray || (declaredType is not null && IsList(declaredType)))
            return "items";
        if (declaredType is null || declaredType == typeof(object) || declaredType == typeof(JsonNode) || declaredType == typeof(JsonObject) || declaredType == typeof(JsonArray))
            return "value";
        var name = declaredType.Name;
        var tick = name.IndexOf('`');
        if (tick > 0)
            name = name[..tick];
        return char.ToLowerInvariant(name[0]) + name[1..];
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

    private static string ScalarText(JsonValue value)
        => value.GetValueKind() switch {
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.Number => value.TryGetValue<long>(out var whole)
                ? whole.ToString(CultureInfo.InvariantCulture)
                : value.GetValue<double>().ToString(CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

    private static object Coerce(string text)
    {
        if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
            return false;
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) && whole.ToString(CultureInfo.InvariantCulture) == text)
            return whole;
        if (text.IndexOfAny(['.', 'e', 'E']) >= 0 && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var real))
            return real;
        return text;
    }

    private static Action<JsonOptions> Copy(XmlOptions options)
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
