using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Query.Models.Parameters;

/// <summary>Binds sibling parameter values into a root <see cref="QueryReq" /> template (<c>{{ParamKey}}</c> placeholders) and extracts key/label pairs from projected query rows.</summary>
public static class ParameterOptionsBinder
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Collects distinct placeholder keys (without braces) from <paramref name="query" /> where values.</summary>
    public static IReadOnlyList<string> GetInputParameterKeys(QueryReq query)
    {
        ArgumentHelpers.ThrowIfNull(query);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectPlaceholders(query.WhereClause, keys);
        CollectPlaceholders(query.From.Query?.WhereClause, keys);
        foreach (var join in query.Joins)
            CollectPlaceholders(join.Query?.WhereClause, keys);

        return keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Clones <paramref name="template" />, substitutes <c>{{Key}}</c> placeholders from <paramref name="siblingValues" />, and returns false when any placeholder lacks a
    /// non-whitespace sibling value.
    /// </summary>
    public static bool TryBind(QueryReq template, IReadOnlyDictionary<string, string?> siblingValues, out QueryReq? bound, out IReadOnlyList<string> missingKeys)
    {
        ArgumentHelpers.ThrowIfNull(template);
        ArgumentHelpers.ThrowIfNull(siblingValues);
        var map = ToIgnoreCaseMap(siblingValues);
        var required = GetInputParameterKeys(template);
        var missing = required.Where(k => !map.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)).ToList();
        if (missing.Count > 0) {
            bound = null;
            missingKeys = missing;
            return false;
        }

        var clone = QueryRequestClone.Clone(template);
        clone.WhereClause = BindWhereClause(clone.WhereClause, map);
        if (clone.From.Query is not null)
            clone.From.Query.WhereClause = BindWhereClause(clone.From.Query.WhereClause, map);

        foreach (var join in clone.Joins) {
            if (join.Query is not null)
                join.Query.WhereClause = BindWhereClause(join.Query.WhereClause, map);
        }

        bound = clone;
        missingKeys = [];
        return true;
    }

    /// <summary>
    /// Reads picker key/label from a projected row. Prefers columns named <c>Key</c>/<c>Value</c>; otherwise uses the property names of the first two
    /// <paramref name="selectPaths" /> (alias stripped).
    /// </summary>
    public static bool TryReadKeyValue(object? row, IReadOnlyList<string>? selectPaths, out string key, out string label)
    {
        key = "";
        label = "";
        if (row is null)
            return false;

        if (TryGetRowProperty(row, "Key", out var keyObj) && TryGetRowProperty(row, "Value", out var labelObj)) {
            key = FormatCell(keyObj);
            label = FormatCell(labelObj);
            return !string.IsNullOrEmpty(key);
        }

        if (selectPaths is { Count: >= 2 }) {
            var keyPath = SelectPropertyName(selectPaths[0]);
            var labelPath = SelectPropertyName(selectPaths[1]);
            if (TryGetRowProperty(row, keyPath, out keyObj) && TryGetRowProperty(row, labelPath, out labelObj)) {
                key = FormatCell(keyObj);
                label = FormatCell(labelObj);
                return !string.IsNullOrEmpty(key);
            }
        }

        if (selectPaths is { Count: 1 }) {
            var only = SelectPropertyName(selectPaths[0]);
            if (TryGetRowProperty(row, only, out var onlyObj)) {
                key = FormatCell(onlyObj);
                label = key;
                return !string.IsNullOrEmpty(key);
            }
        }

        return false;
    }

    /// <summary>Builds static options from JSON-array <c>AllowedValues</c> (key = label for each canonical token).</summary>
    public static IReadOnlyList<ParameterOptionsItem> FromAllowedValues(string? allowedValues)
        => ParameterListJson.Parse(allowedValues).Select(v => new ParameterOptionsItem(v, v)).ToList();

    private static void CollectPlaceholders(WhereClause? clause, HashSet<string> keys)
    {
        switch (clause) {
            case null:
                return;
            case ConditionClause condition:
                CollectFromValue(condition.Value, keys);
                CollectPlaceholders(condition.SubClause, keys);
                break;
            case GroupClause group:
                foreach (var child in group.Children)
                    CollectPlaceholders(child, keys);

                CollectPlaceholders(group.SubClause, keys);
                break;
            default:
                CollectPlaceholders(clause.SubClause, keys);
                break;
        }
    }

    private static void CollectFromValue(object? value, HashSet<string> keys)
    {
        switch (value) {
            case string s:
                foreach (Match m in PlaceholderRegex.Matches(s))
                    keys.Add(m.Groups[1].Value.Trim());

                break;
            case IEnumerable enumerable when value is not string:
                foreach (var item in enumerable)
                    CollectFromValue(item, keys);

                break;
        }
    }

    private static WhereClause? BindWhereClause(WhereClause? clause, IReadOnlyDictionary<string, string?> siblingValues)
        => clause switch {
            null => null,
            ConditionClause condition => new ConditionClause(condition.Field, condition.Comparison, BindValue(condition.Value, siblingValues), condition.Description) {
                SubClause = BindWhereClause(condition.SubClause, siblingValues)
            },
            GroupClause group => new GroupClause(group.Operator, group.Children.Select(c => BindWhereClause(c, siblingValues)!).ToList(), group.Description) {
                SubClause = BindWhereClause(group.SubClause, siblingValues)
            },
            var _ => clause
        };

    private static object? BindValue(object? value, IReadOnlyDictionary<string, string?> siblingValues)
        => value switch {
            string s => ReplacePlaceholders(s, siblingValues),
            IList list => BindList(list, siblingValues),
            IEnumerable enumerable when value is not string => BindList(enumerable.Cast<object?>().ToList(), siblingValues),
            var _ => value
        };

    private static object BindList(IList list, IReadOnlyDictionary<string, string?> siblingValues)
    {
        var result = new List<object?>(list.Count);
        foreach (var item in list)
            result.Add(BindValue(item, siblingValues));

        return result;
    }

    private static Dictionary<string, string?> ToIgnoreCaseMap(IReadOnlyDictionary<string, string?> siblingValues)
    {
        if (siblingValues is Dictionary<string, string?> dict && Equals(dict.Comparer, StringComparer.OrdinalIgnoreCase))
            return dict;

        return siblingValues.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string ReplacePlaceholders(string template, IReadOnlyDictionary<string, string?> siblingValues)
        => PlaceholderRegex.Replace(
            template, m => {
                var key = m.Groups[1].Value.Trim();
                return siblingValues.TryGetValue(key, out var v) && v is not null ? v : m.Value;
            });

    private static string SelectPropertyName(string selectPath)
    {
        var trimmed = selectPath.Trim();
        var dot = trimmed.LastIndexOf('.');
        return dot >= 0 ? trimmed[(dot + 1)..] : trimmed;
    }

    private static bool TryGetRowProperty(object row, string name, out object? value)
    {
        value = null;
        if (string.IsNullOrEmpty(name))
            return false;

        switch (row) {
            case IReadOnlyDictionary<string, object?> roDict:
                return TryGetIgnoreCase(roDict, name, out value);
            case IDictionary<string, object?> dict:
                return TryGetIgnoreCase(dict, name, out value);
            case IDictionary dict: {
                foreach (DictionaryEntry entry in dict) {
                    if (entry.Key is string k && string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) {
                        value = entry.Value;
                        return true;
                    }
                }

                return false;
            }
            case JsonElement { ValueKind: JsonValueKind.Object } el: {
                foreach (var prop in el.EnumerateObject()) {
                    if (!string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    value = prop.Value.ValueKind switch {
                        JsonValueKind.String => prop.Value.GetString(),
                        JsonValueKind.Number => prop.Value.ToString(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null,
                        var _ => prop.Value.ToString()
                    };

                    return true;
                }

                return false;
            }
            default: {
                var prop = row.GetType().GetProperty(name);
                if (prop is null) {
                    prop = row.GetType().GetProperties().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (prop is null)
                        return false;
                }

                value = prop.GetValue(row);
                return true;
            }
        }
    }

    private static bool TryGetIgnoreCase(IEnumerable<KeyValuePair<string, object?>> dict, string name, out object? value)
    {
        foreach (var kv in dict) {
            if (!string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
                continue;

            value = kv.Value;
            return true;
        }

        value = null;
        return false;
    }

    private static string FormatCell(object? value)
        => value switch {
            null => "",
            string s => s,
            JsonElement el => el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture) ?? "",
            var _ => value.ToString() ?? ""
        };
}