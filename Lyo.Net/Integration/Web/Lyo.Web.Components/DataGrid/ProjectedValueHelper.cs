using System.Globalization;
using System.Text.Json;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Helper to extract values from projected query results (JsonElement, Dictionary, or primitive).</summary>
public static class ProjectedValueHelper
{
    public static object? GetValue(object? item, string fieldName)
    {
        if (item == null || string.IsNullOrEmpty(fieldName))
            return null;

        if (item is JsonElement je)
            return GetFromJsonElement(je, fieldName);

        if (item is IReadOnlyDictionary<string, object?> dict)
            return dict.TryGetValue(fieldName, out var v) ? v : dict.TryGetValue(NormalizeKey(fieldName), out v) ? v : null;

        if (item is IDictionary<string, object?> idict)
            return idict.TryGetValue(fieldName, out var v) ? v : idict.TryGetValue(NormalizeKey(fieldName), out v) ? v : null;

        return null;
    }

    public static string GetDisplayValue(object? item, string fieldName)
    {
        var v = GetValue(item, fieldName);
        if (v == null)
            return string.Empty;

        if (v is JsonElement jv) {
            return jv.ValueKind switch {
                JsonValueKind.String => jv.GetString() ?? string.Empty,
                JsonValueKind.Number => jv.GetRawText(),
                JsonValueKind.True => "True",
                JsonValueKind.False => "False",
                JsonValueKind.Null => string.Empty,
                JsonValueKind.Undefined => string.Empty,
                var _ => jv.ToString()
            };
        }

        return v.ToString() ?? string.Empty;
    }

    /// <summary>Best-effort conversion for projected values (JSON numbers often arrive as strings; <see cref="JsonElement"/> is handled).</summary>
    public static long GetInt64(object? value)
    {
        if (TryGetInt64(value, out var l))
            return l;

        return 0;
    }

    /// <inheritdoc cref="GetInt64"/>
    public static bool TryGetInt64(object? value, out long result)
    {
        result = 0;
        if (value is null)
            return false;

        switch (value) {
            case long l:
                result = l;
                return true;
            case int i:
                result = i;
                return true;
            case uint ui:
                result = ui;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case JsonElement je:
                return TryGetInt64FromJson(je, out result);
            case string str when long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out result):
                return true;
            case IConvertible conv:
                try {
                    result = conv.ToInt64(CultureInfo.InvariantCulture);
                    return true;
                }
                catch {
                    return false;
                }
            default:
                return false;
        }
    }

    private static bool TryGetInt64FromJson(JsonElement je, out long result)
    {
        result = 0;
        return je.ValueKind switch {
            JsonValueKind.Number => je.TryGetInt64(out result) || je.TryGetUInt64(out var u) && TryCastULong(u, out result),
            JsonValueKind.String => long.TryParse(je.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result),
            _ => false
        };
    }

    private static bool TryCastULong(ulong u, out long result)
    {
        if (u > long.MaxValue) {
            result = 0;
            return false;
        }

        result = (long)u;
        return true;
    }

    /// <summary>Coerces a projected field value to <typeparamref name="T"/> for typed column formatters.</summary>
    public static T ConvertTo<T>(object? raw)
    {
        if (raw is T ok)
            return ok;

        if (raw is null)
            return default!;

        if (typeof(T) == typeof(long))
            return (T)(object)GetInt64(raw);

        if (raw is JsonElement je) {
            raw = JsonElementToDotNet(je);
            if (raw is T t2)
                return t2;
            if (raw is null)
                return default!;
        }

        try {
            return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture)!;
        }
        catch {
            return default!;
        }
    }

    private static object? JsonElementToDotNet(JsonElement je) => je.ValueKind switch {
        JsonValueKind.String => je.GetString(),
        JsonValueKind.Number => je.TryGetInt64(out var l) ? l : je.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => je
    };

    private static object? GetFromJsonElement(JsonElement je, string fieldName)
    {
        if (je.ValueKind != JsonValueKind.Object)
            return null;

        if (je.TryGetProperty(fieldName, out var prop)) {
            return prop.ValueKind switch {
                JsonValueKind.String => prop.GetString(),
                JsonValueKind.Number => prop.GetRawText(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                var _ => prop
            };
        }

        var normalized = NormalizeKey(fieldName);
        if (je.TryGetProperty(normalized, out prop))
            return prop.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ? null : prop;

        return null;
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key) || char.IsUpper(key[0]))
            return key;

        return char.ToUpperInvariant(key[0]) + key[1..];
    }
}