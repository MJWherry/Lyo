using System.Collections;
using System.Text.Json;

namespace Lyo.Web.Components.DataGrid;

/// <summary>Truncation and filter-value formatting for compact chip labels.</summary>
public static class ChipLabelHelper
{
    public const int DefaultFilterChipMaxLength = 48;

    private const int MaxFilterListItemsShownCompact = 4;

    public static string TruncateLabel(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (maxLength < 1)
            return string.Empty;

        if (text.Length <= maxLength)
            return text;

        const string ellipsis = "...";
        if (maxLength <= ellipsis.Length)
            return ellipsis[..maxLength];

        return text[..(maxLength - ellipsis.Length)] + ellipsis;
    }

    public static bool ShouldTruncate(string? text, int maxLength)
        => !string.IsNullOrEmpty(text) && text.Length > maxLength;

    /// <summary>
    /// Formats a filter condition value (handles <see cref="JsonElement"/>, collections, etc.).
    /// Use <paramref name="compact"/> for chip labels; use <c>false</c> for dialogs that must list every value.
    /// </summary>
    public static string FormatFilterValue(object? value, bool compact = true)
    {
        if (value is null)
            return "null";

        if (value is JsonElement je)
            return FormatJsonElement(je, compact);

        if (value is DateTime dt)
            return dt.ToString("MM/dd/yyyy");

        if (value is DateTimeOffset dto)
            return dto.ToString("g");

        if (value is string s)
            return s;

        if (value is IEnumerable<string> strEnum)
            return FormatFilterStringList(strEnum, compact);

        if (value is IEnumerable<decimal> decEnum)
            return FormatFilterNumberList(decEnum.Select(d => d.ToString("G")), compact);

        if (value is IEnumerable<long> longEnum)
            return FormatFilterNumberList(longEnum.Select(l => l.ToString("G")), compact);

        if (value is IEnumerable<int> intEnum)
            return FormatFilterNumberList(intEnum.Select(i => i.ToString("G")), compact);

        if (value is IEnumerable enumerable and not string) {
            var strings = new List<string>();
            foreach (var item in enumerable)
                strings.Add(item?.ToString() ?? "");

            if (strings.Count > 0)
                return FormatFilterStringList(strings, compact);
        }

        return value.ToString() ?? string.Empty;
    }

    private static string FormatJsonElement(JsonElement je, bool compact)
    {
        return je.ValueKind switch {
            JsonValueKind.Null => "null",
            JsonValueKind.String => je.GetString() ?? "",
            JsonValueKind.Number => je.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => FormatJsonArray(je, compact),
            JsonValueKind.Object => je.ToString(),
            _ => je.ToString()
        };
    }

    private static string FormatJsonArray(JsonElement arr, bool compact)
    {
        var items = new List<string>();
        foreach (var el in arr.EnumerateArray()) {
            var s = el.ValueKind == JsonValueKind.String
                ? el.GetString() ?? ""
                : FormatJsonElement(el, compact);
            items.Add(s);
        }

        return FormatFilterStringList(items, compact);
    }

    private static string FormatFilterStringList(IEnumerable<string> items, bool compact)
    {
        var list = items.Where(static s => !string.IsNullOrEmpty(s)).ToList();
        if (list.Count == 0)
            return "[]";

        if (!compact || list.Count <= MaxFilterListItemsShownCompact)
            return $"[{string.Join(", ", list)}]";

        return $"[{string.Join(", ", list.Take(MaxFilterListItemsShownCompact))}, ... (+{list.Count - MaxFilterListItemsShownCompact} more)]";
    }

    private static string FormatFilterNumberList(IEnumerable<string> formattedNumbers, bool compact)
    {
        var list = formattedNumbers.ToList();
        if (list.Count == 0)
            return "[]";

        if (!compact || list.Count <= MaxFilterListItemsShownCompact)
            return $"[{string.Join(", ", list)}]";

        return $"[{string.Join(", ", list.Take(MaxFilterListItemsShownCompact))}, ... (+{list.Count - MaxFilterListItemsShownCompact} more)]";
    }
}
