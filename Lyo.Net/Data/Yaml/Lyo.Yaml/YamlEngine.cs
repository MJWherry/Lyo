using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lyo.Common.Core.Enums;
using Lyo.Json;
using Lyo.Result;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace Lyo.Yaml;

internal sealed class YamlEngine
{
    private static readonly Regex DocSplit = new(@"(?m)^---[ \t]*$", RegexOptions.Compiled);
    private static readonly Regex Alias = new(@"(?m)(?:^|[\s\[\{,])\*[A-Za-z_]", RegexOptions.Compiled);
    private readonly bool _allowAliases;
    private readonly IDeserializer _deserializer;
    private readonly bool _explicitMarkers;

    private readonly JsonService _json;
    private readonly bool _multiDocument;
    private readonly MemberOrder _order;
    private readonly string[] _orderNames;
    private readonly bool _readNumbersFromStrings;
    private readonly ISerializer _rows;
    private readonly ISerializer _serializer;

    public Encoding Encoding { get; }

    public YamlEngine(YamlOptions options)
    {
        _explicitMarkers = options.ExplicitMarkers;
        _multiDocument = options.MultiDocument;
        _allowAliases = options.AllowAliases;
        _readNumbersFromStrings = options.ReadNumbersFromStrings;
        _order = options.Order;
        _orderNames = options.OrderNames.ToArray();
        Encoding = options.Encoding;
        var block = options.WriteIndented;
        _json = new(Copy(options));
        var deserializer = new DeserializerBuilder().WithAttemptingUnquotedStringTypeDeserialization();
        _deserializer = deserializer.Build();
        var serializer = new SerializerBuilder().WithEventEmitter(next => new YamlStyleEmitter(next, !block, options.QuoteStrings));
        var rows = new SerializerBuilder().WithEventEmitter(next => new YamlStyleEmitter(next, !block, YamlQuoteStyle.DoubleQuoted));
        if (!options.AllowAliases) {
            serializer = serializer.DisableAliases();
            rows = rows.DisableAliases();
        }

        _serializer = serializer.Build();
        _rows = rows.Build();
    }

    public T Parse<T>(string yaml)
    {
        if (_multiDocument && IsList(typeof(T), out var element)) {
            var method = typeof(YamlEngine).GetMethod(nameof(ParseList), BindingFlags.Instance | BindingFlags.NonPublic)!.MakeGenericMethod(element);
            return (T)method.Invoke(this, [yaml])!;
        }

        return FromNode<T>(ToNode(yaml));
    }

    public string Export<T>(T value)
    {
        if (_multiDocument && value is IEnumerable enumerable && value is not string)
            return ExportDocuments(enumerable);

        return Emit(JsonNode.Parse(_json.ExportToJsonString(value)));
    }

    public string ToJson(string yaml) => _json.ExportToJsonString(ToNode(yaml) ?? (object?)null);

    public string FromJson(string json) => Emit(JsonNode.Parse(json));

    public JsonNode? ToNode(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return null;

        if (!_allowAliases && Alias.IsMatch(yaml))
            throw new YamlException("Aliases are disabled.");

        var graph = _deserializer.Deserialize<object>(yaml);
        var node = JsonNodeGraph.FromClr(graph);
        if (_readNumbersFromStrings)
            JsonNodeNumbers.Coerce(node);

        return node;
    }

    private string Emit(JsonNode? node, bool? markers = null) => Emit(node, markers ?? _explicitMarkers, true);

    public string ExportPreservingKeys<T>(T value) => Emit(JsonNode.Parse(_json.ExportPreservingKeyOrder(value)), _explicitMarkers, false);

    /// <summary>Writes a data-table row array. String cells are double-quoted so a value such as <c>01</c> stays text.</summary>
    public string ExportRows(JsonArray rows) => Emit(rows, _rows, _explicitMarkers, false);

    /// <summary>Reads a YAML sequence of maps into a data table. Cell text stays text.</summary>
    public Result<DataTable.Models.DataTable> ParseRows(string yaml, bool hasFooterRow) => DataTableDocument.FromRows(ToNode(yaml), hasFooterRow);

    private string Emit(JsonNode? node, bool mark, bool applyOrder) => Emit(node, _serializer, mark, applyOrder);

    private string Emit(JsonNode? node, ISerializer serializer, bool mark, bool applyOrder)
    {
        if (applyOrder)
            node = JsonMemberOrder.Apply(node, _order, _orderNames);

        if (node is null)
            return mark ? "---\nnull\n...\n" : "null\n";

        var text = serializer.Serialize(JsonNodeGraph.ToClr(node));
        if (!mark)
            return text;

        return "---\n" + text.TrimEnd() + "\n...\n";
    }

    private T FromNode<T>(JsonNode? node)
    {
        if (node is null)
            return default!;

        return _json.ParseString<T>(node.ToJsonString());
    }

    private List<TItem> ParseList<TItem>(string yaml)
    {
        var list = new List<TItem>();
        foreach (var chunk in Split(yaml))
            list.Add(FromNode<TItem>(ToNode(chunk)));

        return list;
    }

    private string ExportDocuments(IEnumerable values)
    {
        var builder = new StringBuilder();
        foreach (var value in values) {
            builder.Append("---\n");
            var node = value is null ? null : JsonNode.Parse(_json.ExportToJsonString(value));
            builder.Append(Emit(node, false).TrimEnd());
            builder.Append('\n');
        }

        if (_explicitMarkers)
            builder.Append("...\n");

        return builder.ToString();
    }

    private static IEnumerable<string> Split(string yaml)
    {
        foreach (var part in DocSplit.Split(yaml)) {
            var trimmed = part.Trim();
            if (trimmed.Length == 0 || trimmed == "...")
                continue;

            yield return trimmed;
        }
    }

    private static bool IsList(Type type, out Type element)
    {
        element = typeof(object);
        if (type == typeof(string))
            return false;

        if (type.IsArray) {
            element = type.GetElementType()!;
            return false;
        }

        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(List<>))
            return false;

        element = type.GetGenericArguments()[0];
        return true;
    }

    private static Action<JsonOptions> Copy(YamlOptions options)
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

internal sealed class YamlStyleEmitter : ChainedEventEmitter
{
    private readonly bool _flow;
    private readonly YamlQuoteStyle _quotes;

    public YamlStyleEmitter(IEventEmitter next, bool flow, YamlQuoteStyle quotes)
        : base(next)
    {
        _flow = flow;
        _quotes = quotes;
    }

    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
    {
        if (_flow)
            eventInfo.Style = MappingStyle.Flow;

        base.Emit(eventInfo, emitter);
    }

    public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
    {
        if (_flow)
            eventInfo.Style = SequenceStyle.Flow;

        base.Emit(eventInfo, emitter);
    }

    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if (_quotes != YamlQuoteStyle.Plain && eventInfo.Source.Type == typeof(string))
            eventInfo.Style = _quotes == YamlQuoteStyle.SingleQuoted ? ScalarStyle.SingleQuoted : ScalarStyle.DoubleQuoted;

        base.Emit(eventInfo, emitter);
    }
}