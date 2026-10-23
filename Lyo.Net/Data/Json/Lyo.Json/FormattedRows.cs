using System.Reflection;
using Lyo.Exceptions;

namespace Lyo.Json;

/// <summary>Projects caller-ordered export fields. Dictionary order is the written order.</summary>
internal static class FormattedRows
{
    public static List<Dictionary<string, string>> Strings<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields)
    {
        ArgumentHelpers.ThrowIfNull(items);
        ArgumentHelpers.ThrowIfNullOrEmpty(fields);
        var names = new List<string>(fields.Count);
        var formatters = new List<Func<T, string>>(fields.Count);
        foreach (var field in fields) {
            ArgumentHelpers.ThrowIfNull(field.Value);
            names.Add(field.Key);
            formatters.Add(field.Value);
        }

        var rows = new List<Dictionary<string, string>>();
        foreach (var item in items) {
            var row = new Dictionary<string, string>(names.Count, StringComparer.Ordinal);
            for (var i = 0; i < names.Count; i++)
                row[names[i]] = formatters[i](item) ?? string.Empty;

            rows.Add(row);
        }

        return rows;
    }

    public static List<Dictionary<string, object?>> Values<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns)
    {
        ArgumentHelpers.ThrowIfNull(items);
        ArgumentHelpers.ThrowIfNullOrEmpty(columns);
        var names = new List<string>(columns.Count);
        var properties = new List<PropertyInfo>(columns.Count);
        foreach (var column in columns) {
            ArgumentHelpers.ThrowIfNull(column.Value);
            if (!column.Value.CanRead)
                throw new ArgumentException($"Property {column.Value.Name} is not readable.", nameof(columns));

            names.Add(column.Key);
            properties.Add(column.Value);
        }

        var rows = new List<Dictionary<string, object?>>();
        foreach (var item in items) {
            var row = new Dictionary<string, object?>(names.Count, StringComparer.Ordinal);
            for (var i = 0; i < names.Count; i++)
                row[names[i]] = item is null ? null : properties[i].GetValue(item);

            rows.Add(row);
        }

        return rows;
    }
}