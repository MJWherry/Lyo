using System.Globalization;
using System.Text.Json;
using Lyo.Common.Conversion;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Formats projected timestamps for grid cells (UTC ISO → local general date/time).</summary>
public static class LyoDateTimeDisplay
{
    /// <summary>Local general date/time (<c>g</c>), or an em dash when the value is missing/unparseable. Use as <c>FormattedValue</c>.</summary>
    public static string FormatProjected(object? _, object? value) => Format(value);

    /// <summary>Latest timestamp in a projected collection, formatted like <see cref="FormatProjected" />.</summary>
    public static string FormatLatestProjected(object? _, object? value) => Format(Latest(value));

    /// <summary>Local general date/time, or an em dash when unset.</summary>
    public static string Format(object? value)
    {
        var dt = ToDateTime(value);
        if (dt is null)
            return "—";

        var utc = dt.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : dt.Value;
        var local = utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
        return local.ToString("g", CultureInfo.CurrentCulture);
    }

    /// <summary>Newest timestamp in a scalar, JSON array, or collection of projected values.</summary>
    public static DateTime? Latest(object? value)
    {
        DateTime? best = null;
        foreach (var item in Enumerate(value)) {
            var dt = ToDateTime(item);
            if (dt is null)
                continue;

            if (best is null || dt.Value > best.Value)
                best = dt;
        }

        return best;
    }

    /// <summary>Parses a projected JSON/CLR value to UTC <see cref="DateTime" />.</summary>
    public static DateTime? ToDateTime(object? value)
    {
        switch (value) {
            case null:
                return null;
            case DateTime dt:
                return dt;
            case DateTimeOffset dto:
                return dto.UtcDateTime;
            case JsonElement { ValueKind: JsonValueKind.String } je:
                return ToDateTime(je.GetString());
            case JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined }:
                return null;
            case string s when DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedOffset):
                return parsedOffset.UtcDateTime;
            case string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed):
                return parsed;
            default:
                return TypeConversion.TryConvertTo<DateTime?>(value, out var converted) ? converted : null;
        }
    }

    private static IEnumerable<object?> Enumerate(object? value)
    {
        switch (value) {
            case null:
                yield break;
            case string:
                yield return value;
                yield break;
            case JsonElement { ValueKind: JsonValueKind.Array } arr:
                foreach (var el in arr.EnumerateArray())
                    yield return el;
                yield break;
            case IEnumerable<object?> objects:
                foreach (var item in objects)
                    yield return item;
                yield break;
            case System.Collections.IEnumerable enumerable:
                foreach (var item in enumerable)
                    yield return item;
                yield break;
            default:
                yield return value;
                break;
        }
    }
}
