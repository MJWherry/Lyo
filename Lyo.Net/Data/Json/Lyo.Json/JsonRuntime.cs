using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Lyo.Common.Core.Enums;
using Lyo.Json.JsonConverters;

namespace Lyo.Json;

internal sealed class JsonRuntime
{
    public JsonSerializerOptions Serializer { get; }

    public JsonSerializerOptions Compact { get; }

    public Encoding Encoding { get; }

    public bool ReadNumbersFromStrings { get; }

    public bool IgnoreNulls { get; }

    public bool IgnoreEmptyStrings { get; }

    public bool IgnoreEmptyCollections { get; }

    public MemberOrder Order { get; }

    public string[] OrderNames { get; }

    public JsonNamingPolicy? Naming { get; }

    private JsonRuntime(
        JsonSerializerOptions serializer,
        JsonSerializerOptions compact,
        Encoding encoding,
        bool readNumbersFromStrings,
        bool ignoreNulls,
        bool ignoreEmptyStrings,
        bool ignoreEmptyCollections,
        MemberOrder order,
        string[] orderNames,
        JsonNamingPolicy? naming)
    {
        Serializer = serializer;
        Compact = compact;
        Encoding = encoding;
        ReadNumbersFromStrings = readNumbersFromStrings;
        IgnoreNulls = ignoreNulls;
        IgnoreEmptyStrings = ignoreEmptyStrings;
        IgnoreEmptyCollections = ignoreEmptyCollections;
        Order = order;
        OrderNames = orderNames;
        Naming = naming;
    }

    public static JsonRuntime FromOptions(JsonOptions options)
    {
        var serializer = Build(options);
        var names = options.OrderNames.ToArray();
        return new(
            serializer, CompactOf(serializer), options.Encoding, options.ReadNumbersFromStrings, options.IgnoreNulls, options.IgnoreEmptyStrings, options.IgnoreEmptyCollections,
            options.Order, names, NamingPolicy(options.Naming));
    }

    public static JsonRuntime FromHost(JsonSerializerOptions serializerOptions)
    {
        var copy = new JsonSerializerOptions(serializerOptions);
        Seal(copy);
        return new(copy, CompactOf(copy), new UTF8Encoding(false), false, true, false, false, MemberOrder.Defined, [], null);
    }

    public string Serialize<T>(T value) => Serialize(value, Serializer);

    /// <summary>Serializes with this service's contract and does not apply <see cref="MemberOrder" /> or empty-value pruning.</summary>
    internal string SerializePreservingKeys<T>(T value) => JsonSerializer.Serialize(value, Serializer);

    public string SerializeCompact<T>(T value) => Serialize(value, Compact);

    public byte[] SerializeBytes<T>(T value, Encoding? encoding)
    {
        var effective = encoding ?? Encoding;
        if (IsUtf8(effective) && !IgnoreEmptyStrings && !IgnoreEmptyCollections && Order == MemberOrder.Defined)
            return JsonSerializer.SerializeToUtf8Bytes(value, Serializer);

        return effective.GetBytes(Serialize(value));
    }

    public T Deserialize<T>(string json)
    {
        var value = JsonSerializer.Deserialize<T>(json, Serializer)!;
        return Coerce(value);
    }

    public T Deserialize<T>(ReadOnlySpan<char> json)
    {
        var value = JsonSerializer.Deserialize<T>(json, Serializer)!;
        return Coerce(value);
    }

    public T DeserializeUtf8<T>(ReadOnlySpan<byte> json)
    {
        var value = JsonSerializer.Deserialize<T>(json, Serializer)!;
        return Coerce(value);
    }

    public T DeserializeBytes<T>(ReadOnlySpan<byte> json, Encoding? encoding)
    {
        var effective = encoding ?? Encoding;
        if (IsUtf8(effective))
            return DeserializeUtf8<T>(json);

        return Deserialize<T>(Decode(effective, json));
    }

    public static bool IsUtf8(Encoding encoding) => encoding.CodePage == 65001;

    public static string Decode(Encoding encoding, ReadOnlySpan<byte> data)
    {
        var preamble = encoding.GetPreamble();
        if (preamble.Length > 0 && data.StartsWith(preamble))
            data = data.Slice(preamble.Length);

        var bytes = data.ToArray();
        return encoding.GetString(bytes);
    }

    private string Serialize<T>(T value, JsonSerializerOptions options)
    {
        if (Order == MemberOrder.Defined && !IgnoreEmptyStrings && !IgnoreEmptyCollections)
            return JsonSerializer.Serialize(value, options);

        var node = JsonSerializer.SerializeToNode(value, options);
        if (IgnoreEmptyStrings || IgnoreEmptyCollections)
            JsonNodePrune.Apply(node, IgnoreNulls, IgnoreEmptyStrings, IgnoreEmptyCollections);

        if (Order != MemberOrder.Defined)
            node = JsonMemberOrder.Apply(node, Order, OrderNames, typeof(T), Naming);

        return node is null ? "null" : node.ToJsonString(options);
    }

    private T Coerce<T>(T value)
    {
        if (ReadNumbersFromStrings && value is JsonNode node)
            JsonNumberCoercion.Apply(node);

        return value;
    }

    private static JsonSerializerOptions CompactOf(JsonSerializerOptions serializer)
    {
        var compact = new JsonSerializerOptions(serializer) { WriteIndented = false };
        Seal(compact);
        return compact;
    }

    private static JsonSerializerOptions Build(JsonOptions options)
    {
        var serializer = LyoJsonSerializerOptions.Create();
        var naming = NamingPolicy(options.Naming);
        serializer.PropertyNamingPolicy = naming;
        serializer.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive;
        serializer.WriteIndented = options.WriteIndented;
        serializer.MaxDepth = options.MaxDepth;
        serializer.AllowTrailingCommas = options.AllowTrailingCommas;
        serializer.ReadCommentHandling = options.CommentHandling == DocumentCommentHandling.Skip ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow;
        serializer.DefaultIgnoreCondition = options.IgnoreDefaults ? JsonIgnoreCondition.WhenWritingDefault :
            options.IgnoreNulls ? JsonIgnoreCondition.WhenWritingNull : JsonIgnoreCondition.Never;

        serializer.UnmappedMemberHandling = options.UnmappedMembers == DocumentUnmappedMembers.Error ? JsonUnmappedMemberHandling.Disallow : JsonUnmappedMemberHandling.Skip;
        if (options.WriteNumbersAsStrings)
            serializer.NumberHandling |= JsonNumberHandling.WriteAsString;

        ReplaceEnumConverter(serializer, options.Enums, naming);
        if (options.EmptyStringAsNull)
            serializer.Converters.Add(new EmptyStringAsNullConverter());

        if (options.DateTimeStyle == DocumentDateTimeStyle.UnixMilliseconds) {
            serializer.Converters.Add(new UnixMillisecondsDateTimeConverter());
            serializer.Converters.Add(new UnixMillisecondsDateTimeOffsetConverter());
        }

        if (options.UseStringScalarConverters)
            AddStringConverters(serializer);

        Seal(serializer);
        return serializer;
    }

    private static void Seal(JsonSerializerOptions serializer)
    {
        serializer.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
        if (!serializer.IsReadOnly)
            serializer.MakeReadOnly();
    }

    private static JsonNamingPolicy? NamingPolicy(DocumentNaming naming)
        => naming switch {
            DocumentNaming.PascalCase => null,
            DocumentNaming.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            DocumentNaming.KebabCase => JsonNamingPolicy.KebabCaseLower,
            var _ => JsonNamingPolicy.CamelCase
        };

    private static void ReplaceEnumConverter(JsonSerializerOptions serializer, DocumentEnumFormat enums, JsonNamingPolicy? naming)
    {
        for (var i = serializer.Converters.Count - 1; i >= 0; i--) {
            if (serializer.Converters[i] is JsonStringEnumConverter)
                serializer.Converters.RemoveAt(i);
        }

        if (enums == DocumentEnumFormat.String)
            serializer.Converters.Add(new JsonStringEnumConverter(naming));
    }

    private static void AddStringConverters(JsonSerializerOptions serializer)
    {
        serializer.Converters.Add(new StringIntConverter());
        serializer.Converters.Add(new StringIntNullableConverter());
        serializer.Converters.Add(new StringLongConverter());
        serializer.Converters.Add(new StringLongNullableConverter());
        serializer.Converters.Add(new StringDoubleConverter());
        serializer.Converters.Add(new StringDecimalConverter());
        serializer.Converters.Add(new StringDecimalNullableConverter());
        serializer.Converters.Add(new StringIntBoolConverter());
        serializer.Converters.Add(new StringIntBoolNullableConverter());
        serializer.Converters.Add(new StringDateTimeConverter("O"));
        serializer.Converters.Add(new ObjectJsonConverter());
    }
}

internal static class JsonNumberCoercion
{
    public static void Apply(JsonNode? node)
    {
        switch (node) {
            case JsonObject obj:
                foreach (var name in obj.Select(static pair => pair.Key).ToArray()) {
                    var child = obj[name];
                    if (child is JsonValue value && TryNumber(value, out var number))
                        obj[name] = number;
                    else
                        Apply(child);
                }

                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++) {
                    var child = array[i];
                    if (child is JsonValue value && TryNumber(value, out var number))
                        array[i] = number;
                    else
                        Apply(child);
                }

                break;
        }
    }

    private static bool TryNumber(JsonValue value, out JsonNode? number)
    {
        number = null;
        if (value.GetValueKind() != JsonValueKind.String)
            return false;

        var text = value.GetValue<string>();
        if (string.IsNullOrEmpty(text))
            return false;

        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole) && whole.ToString(CultureInfo.InvariantCulture) == text) {
            number = whole is >= int.MinValue and <= int.MaxValue ? JsonValue.Create((int)whole) : JsonValue.Create(whole);
            return number is not null;
        }

        if (text.IndexOfAny(['.', 'e', 'E']) >= 0 && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var real) &&
            (real.ToString("R", CultureInfo.InvariantCulture) == text || real.ToString("G", CultureInfo.InvariantCulture) == text)) {
            number = JsonValue.Create(real);
            return number is not null;
        }

        return false;
    }
}

internal static class JsonNodePrune
{
    public static void Apply(JsonNode? node, bool ignoreNulls, bool ignoreEmptyStrings, bool ignoreEmptyCollections)
    {
        if (node is JsonObject obj) {
            foreach (var name in obj.Select(static pair => pair.Key).ToArray()) {
                Apply(obj[name], ignoreNulls, ignoreEmptyStrings, ignoreEmptyCollections);
                if (ShouldRemove(obj[name], ignoreNulls, ignoreEmptyStrings, ignoreEmptyCollections))
                    obj.Remove(name);
            }
        }
        else if (node is JsonArray array) {
            foreach (var child in array)
                Apply(child, ignoreNulls, ignoreEmptyStrings, ignoreEmptyCollections);
        }
    }

    private static bool ShouldRemove(JsonNode? node, bool ignoreNulls, bool ignoreEmptyStrings, bool ignoreEmptyCollections)
    {
        if (node is null)
            return ignoreNulls;

        if (ignoreEmptyStrings && node is JsonValue value && value.GetValueKind() == JsonValueKind.String && value.GetValue<string>() == "")
            return true;

        if (!ignoreEmptyCollections)
            return false;

        return node switch {
            JsonArray array => array.Count == 0,
            JsonObject obj => obj.Count == 0,
            var _ => false
        };
    }
}

internal sealed class EmptyStringAsNullConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var text = reader.GetString();
        return text is "" ? null : text;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}

internal sealed class UnixMillisecondsDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64()).UtcDateTime;

        return reader.GetDateTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
        writer.WriteNumberValue(new DateTimeOffset(utc).ToUnixTimeMilliseconds());
    }
}

internal sealed class UnixMillisecondsDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());

        return reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) => writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
}