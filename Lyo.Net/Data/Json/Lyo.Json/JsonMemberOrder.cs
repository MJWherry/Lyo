using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Lyo.Common.Core;
using Lyo.Common.Core.Attributes;
using Lyo.Common.Core.Enums;

namespace Lyo.Json;

/// <summary>Reorders object keys for <see cref="MemberOrder" />. Defined leaves the node alone.</summary>
public static class JsonMemberOrder
{
    /// <summary>Reorders <paramref name="node" /> and its nested objects. Names in <paramref name="orderNames" /> are written names.</summary>
    public static JsonNode? Apply(JsonNode? node, MemberOrder order, IReadOnlyList<string> orderNames) => Apply(node, order, orderNames, null, null);

    internal static JsonNode? Apply(JsonNode? node, MemberOrder order, IReadOnlyList<string> orderNames, Type? type, JsonNamingPolicy? naming)
    {
        if (order == MemberOrder.Defined || node is null)
            return node;

        ApplyCore(node, order, orderNames, type, naming);
        return node;
    }

    private static void ApplyCore(JsonNode? node, MemberOrder order, IReadOnlyList<string> orderNames, Type? type, JsonNamingPolicy? naming)
    {
        switch (node) {
            case JsonObject obj:
                Reorder(obj, order, orderNames, type, naming);
                foreach (var property in obj) {
                    var child = ChildType(type, property.Key, naming);
                    ApplyCore(property.Value, order, orderNames, child, naming);
                }

                break;
            case JsonArray array:
                var element = ElementType(type);
                foreach (var item in array)
                    ApplyCore(item, order, orderNames, element, naming);

                break;
        }
    }

    private static void Reorder(JsonObject obj, MemberOrder order, IReadOnlyList<string> orderNames, Type? type, JsonNamingPolicy? naming)
    {
        if (obj.Count <= 1)
            return;

        var attributes = AttributeOrders(type, naming);
        var pairs = new List<KeyValuePair<string, JsonNode?>>(obj.Count);
        foreach (var property in obj)
            pairs.Add(property);

        var sorted = MemberOrderSort.Sort(pairs, static pair => pair.Key, pair => attributes.TryGetValue(pair.Key, out var rank) ? rank : null, order, orderNames);
        foreach (var property in obj.ToList())
            obj.Remove(property.Key);

        foreach (var pair in sorted)
            obj[pair.Key] = pair.Value;
    }

    private static Dictionary<string, int> AttributeOrders(Type? type, JsonNamingPolicy? naming)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        if (type is null || type == typeof(object) || type == typeof(JsonObject) || type == typeof(JsonNode) || type == typeof(JsonArray))
            return map;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead)
                continue;

            var attribute = property.GetCustomAttribute<MemberOrderAttribute>();
            if (attribute is null)
                continue;

            map[SerializedName(property, naming)] = attribute.Order;
        }

        return map;
    }

    private static Type? ChildType(Type? type, string key, JsonNamingPolicy? naming)
    {
        if (type is null)
            return null;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
            if (property.GetIndexParameters().Length != 0 || !property.CanRead)
                continue;

            if (SerializedName(property, naming) != key)
                continue;

            return Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        }

        return null;
    }

    private static Type? ElementType(Type? type)
    {
        if (type is null || type == typeof(string))
            return null;

        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsArray)
            return type.GetElementType();

        foreach (var iface in type.GetInterfaces()) {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return iface.GetGenericArguments()[0];
        }

        return null;
    }

    internal static string SerializedName(PropertyInfo property, JsonNamingPolicy? naming)
    {
        var named = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (named is not null && !string.IsNullOrEmpty(named.Name))
            return named.Name;

        return naming?.ConvertName(property.Name) ?? property.Name;
    }
}