using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lyo.Json;

/// <summary>Converts between a <see cref="JsonNode" /> and a CLR object graph of dictionaries, lists, and scalars. YAML, XML, and TOML use this so they share the Lyo JSON contract.</summary>
public static class JsonNodeGraph
{
    /// <summary>Builds a <see cref="JsonNode" /> from a dictionary, list, or scalar. A null value returns null.</summary>
    public static JsonNode? FromClr(object? value)
    {
        switch (value) {
            case null:
                return null;
            case JsonNode node:
                return node;
            case bool b:
                return JsonValue.Create(b);
            case string s:
                return JsonValue.Create(s);
            case sbyte or byte or short or ushort or int or uint or long:
                return JsonValue.Create(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            case ulong ul when ul <= long.MaxValue:
                return JsonValue.Create((long)ul);
            case float or double or decimal:
                return JsonValue.Create(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            case DateTime dt:
                return JsonValue.Create(dt);
            case DateTimeOffset dto:
                return JsonValue.Create(dto);
            case IDictionary dictionary:
                return FromDictionary(dictionary);
            case IEnumerable<KeyValuePair<string, object>> pairs:
                return FromPairs(pairs);
            case IEnumerable list:
                return FromList(list);
            default:
                return JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Builds a dictionary, list, or scalar from <paramref name="node" />. A null node returns null.</summary>
    public static object? ToClr(JsonNode? node)
        => node switch {
            null => null,
            JsonObject obj => ToDictionary(obj),
            JsonArray array => array.Select(ToClr).ToList(),
            JsonValue value => ScalarFromValue(value),
            var _ => null
        };

    private static JsonObject FromDictionary(IDictionary dictionary)
    {
        var obj = new JsonObject();
        foreach (DictionaryEntry entry in dictionary) {
            var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
            if (key is null)
                continue;

            obj[key] = FromClr(entry.Value);
        }

        return obj;
    }

    private static JsonObject FromPairs(IEnumerable<KeyValuePair<string, object>> pairs)
    {
        var obj = new JsonObject();
        foreach (var pair in pairs)
            obj[pair.Key] = FromClr(pair.Value);

        return obj;
    }

    private static JsonArray FromList(IEnumerable list)
    {
        var array = new JsonArray();
        foreach (var item in list)
            array.Add(FromClr(item));

        return array;
    }

    private static Dictionary<string, object?> ToDictionary(JsonObject obj)
    {
        var dictionary = new Dictionary<string, object?>(obj.Count, StringComparer.Ordinal);
        foreach (var property in obj)
            dictionary[property.Key] = ToClr(property.Value);

        return dictionary;
    }

    private static object? ScalarFromValue(JsonValue value)
        => value.GetValueKind() switch {
            JsonValueKind.String => value.GetValue<string>(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Number => value.TryGetValue<long>(out var whole) ? whole : value.GetValue<double>(),
            var _ => value.ToString()
        };
}