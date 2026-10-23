using System.Collections;
using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>One predicate: dotted <see cref="Field" />, <see cref="Comparison" />, and <see cref="Value" /> (scalar, list, or JSON-compatible literal).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class ConditionClause : WhereClause, IEquatable<ConditionClause>
{
    /// <summary>Default Fuzzy / NotFuzzy cutoff when <see cref="Threshold" /> is omitted. Matches Postgres <c>pg_trgm.similarity_threshold</c>.</summary>
    public const double DefaultFuzzyThreshold = 0.3;

    /// <summary>Dotted property path on the entity (optional collection navigation), e.g. <c>Status</c> or <c>Lines.Quantity</c>.</summary>
    public string Field { get; set; }

    /// <summary>Comparison applied to the field and value.</summary>
    public ComparisonOperatorEnum Comparison { get; set; }

    /// <summary>
    /// Right-hand value: type depends on <see cref="Comparison" /> (CSV strings allowed for <see cref="ComparisonOperatorEnum.In" /> and
    /// <see cref="ComparisonOperatorEnum.NotIn" />).
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Similarity cutoff for <see cref="ComparisonOperatorEnum.Fuzzy" /> / <see cref="ComparisonOperatorEnum.NotFuzzy" /> in <c>[0, 1]</c>. Omitted uses
    /// <see cref="DefaultFuzzyThreshold" /> (Postgres <c>pg_trgm.similarity_threshold</c> default). Ignored for every other operator.
    /// </summary>
    public double? Threshold { get; set; }

    /// <summary>Builds a condition with an empty field and default <see cref="ComparisonOperatorEnum.Equals" />.</summary>
    public ConditionClause()
    {
        Field = string.Empty;
        Comparison = ComparisonOperatorEnum.Equals;
        Value = null!;
    }

    /// <summary>Builds a condition with the given field, operator, value, optional description, and optional Fuzzy threshold.</summary>
    /// <param name="field">Dotted property path.</param>
    /// <param name="comparison">Comparison operator.</param>
    /// <param name="value">Filter literal.</param>
    /// <param name="description">Optional <see cref="WhereClause.Description" />.</param>
    /// <param name="threshold">Optional Fuzzy / NotFuzzy cutoff in <c>[0, 1]</c>.</param>
    public ConditionClause(string field, ComparisonOperatorEnum comparison, object? value, string? description = null, double? threshold = null)
    {
        Field = field;
        Comparison = comparison;
        Value = value;
        Description = description;
        Threshold = threshold;
    }

    public bool Equals(ConditionClause? other)
        => other is not null && Field == other.Field && Comparison == other.Comparison && Nullable.Equals(Threshold, other.Threshold) && ValueEquals(Value, other.Value);

    public override string Print(int indent = 0)
    {
        var pad = new string(' ', indent * 2);

        // For collections, print each item on its own indented line.
        if (Value is not string && Value is IEnumerable many) {
            var items = many.Cast<object?>().Select(x => x?.ToString()).ToList();
            return items.Count switch {
                0 => $"{pad}{Field} {Comparison} []",
                1 => $"{pad}{Field} {Comparison} '{items[0]}'",
                var _ => $"{pad}{Field} {Comparison} [\n{string.Join("\n", items.Select(i => $"{(' ', (indent + 1) * 2)}- {i ?? "NULL"}"))}\n{pad}]"
            };
        }

        // Scalar value.
        return $"{pad}{Field} {Comparison} {(Value is not null ? $"'{Value}'" : "NULL")}{ThresholdSuffix()}";
    }

    public override string ToString()
    {
        if (Description is not null)
            return Description;

        if (Value is string || Value is not IEnumerable many)
            return $"{Field} {Comparison} {(Value is not null ? $"'{Value}'" : "NULL")}{ThresholdSuffix()}";

        var items = many.Cast<object?>().Select(x => x?.ToString() ?? string.Empty);
        return $"{Field} {Comparison} '{string.Join(",", items)}'{ThresholdSuffix()}";
    }

    public override bool Equals(object? obj) => Equals(obj as ConditionClause);

    public override int GetHashCode() => HashCodeHelpers.Combine(Field, Comparison, ValueHash(Value), Threshold);

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

    private string ThresholdSuffix() => Comparison is ComparisonOperatorEnum.Fuzzy or ComparisonOperatorEnum.NotFuzzy && Threshold is { } t ? $" threshold={t}" : string.Empty;
}