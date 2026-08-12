using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lyo.Common.Extensions;

namespace Lyo.Query.Models.Parameters;

/// <summary>
/// Serialize / parse definition list payloads stored in string columns (<c>AllowedValues</c>, multi <c>Value</c>). Payload is a JSON array of typed primitives; parse always
/// yields canonical invariant strings for UI and validation.
/// </summary>
public static class ParameterListJson
{
    /// <summary>
    /// Deserializes a JSON array into canonical string forms. Null/whitespace or invalid/non-array JSON yields an empty list. Accepts string, number, and boolean elements; null
    /// elements are skipped. No pipe-separated fallback.
    /// </summary>
    public static IReadOnlyList<string> Parse(string? json)
    {
        if (json.IsNullOrWhitespace())
            return [];

        try {
            using var doc = JsonDocument.Parse(json!);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<string>();
            foreach (var el in doc.RootElement.EnumerateArray()) {
                var formatted = FormatElement(el);
                if (formatted.Length > 0)
                    list.Add(formatted);
            }

            return list;
        }
        catch (JsonException) {
            return [];
        }
    }

    /// <summary>
    /// Serializes chip/string values to a JSON array. Returns null when there are no non-empty values after kind filtering. <paramref name="kind" /> selects JSON string, number,
    /// or bool wire shape.
    /// </summary>
    public static string? Serialize(IEnumerable<string>? values, ParameterListJsonKind kind = ParameterListJsonKind.String)
    {
        if (values is null)
            return null;

        var array = new JsonArray();
        foreach (var raw in values) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var trimmed = raw.Trim();
            switch (kind) {
                case ParameterListJsonKind.Number:
                    if (!decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                        continue;

                    array.Add(JsonValue.Create(number));
                    break;
                case ParameterListJsonKind.Bool:
                    if (!bool.TryParse(trimmed, out var flag))
                        continue;

                    array.Add(JsonValue.Create(flag));
                    break;
                default:
                    array.Add(trimmed);
                    break;
            }
        }

        return array.Count == 0 ? null : array.ToJsonString();
    }

    private static string FormatElement(JsonElement el)
        => el.ValueKind switch {
            JsonValueKind.String => (el.GetString() ?? "").Trim(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            var _ => ""
        };
}