using System.Collections;
using System.Diagnostics;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common;

[DebuggerDisplay("{ToString(),nq}")]
public class ConditionClause : WhereClause, IEquatable<ConditionClause>
{
    public string Field { get; set; }
    
    public ComparisonOperatorEnum Comparison { get; set; }

    public object? Value { get; set; }

    public ConditionClause()
    {
        Field = string.Empty;
        Comparison = ComparisonOperatorEnum.Equals;
        Value = null!;
    }

    public ConditionClause(string field, ComparisonOperatorEnum comparison, object? value, string? description = null)
    {
        Field = field;
        Comparison = comparison;
        Value = value;
        Description = description;
    }

    public bool Equals(ConditionClause? other) => other is not null && Field == other.Field && Comparison == other.Comparison && ValueEquals(Value, other.Value);

    public override string Print(int indent = 0)
    {
        var pad = new string(' ', indent * 2);

        // For collections, print each item on its own indented line for readability
        if (Value is not string && Value is IEnumerable many) {
            var items = many.Cast<object?>().Select(x => x?.ToString() ?? string.Empty).ToList();
            if (items.Count == 0)
                return $"{pad}{Field} {Comparison} []";

            if (items.Count == 1)
                return $"{pad}{Field} {Comparison} '{items[0]}'";

            var innerPad = new string(' ', (indent + 1) * 2);
            var joined = string.Join("\n", items.Select(i => $"{innerPad}- {i ?? "NULL"}"));
            return $"{pad}{Field} {Comparison} [\n{joined}\n{pad}]";
        }

        // Simple value
        return $"{pad}{Field} {Comparison} {(Value is not null ? $"'{Value}'" : "NULL")}";
    }

    public override string ToString()
    {
        if (Description is not null)
            return Description;

        if (Value is string || Value is not IEnumerable many)
            return $"{Field} {Comparison} {(Value is not null ? $"'{Value}'" : "NULL")}";

        var items = many.Cast<object?>().Select(x => x?.ToString() ?? string.Empty);
        return $"{Field} {Comparison} '{string.Join(",", items)}'";
    }

    public override bool Equals(object? obj) => Equals(obj as ConditionClause);

    public override int GetHashCode()
    {
        unchecked {
            var hash = 17;
            hash = hash * 31 + (Field?.GetHashCode() ?? 0);
            hash = hash * 31 + Comparison.GetHashCode();
            hash = hash * 31 + ValueHash(Value);
            return hash;
        }
    }

    private static bool ValueEquals(object? a, object? b)
    {
        if (a == b)
            return true;

        if (a is null || b is null)
            return false;

        if (a is IEnumerable ea && b is IEnumerable eb)
            return ea.Cast<object?>().SequenceEqual(eb.Cast<object?>());

        return Equals(a, b);
    }

    private static int ValueHash(object? v)
    {
        if (v is null)
            return 0;

        if (v is IEnumerable e)
            return e.Cast<object?>().Aggregate(0, (h, x) => unchecked(h * 31 + (x?.GetHashCode() ?? 0)));

        return v.GetHashCode();
    }
}
