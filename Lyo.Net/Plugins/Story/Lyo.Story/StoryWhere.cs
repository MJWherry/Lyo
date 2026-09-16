using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>Combines, negates, and evaluates <see cref="WhereClause"/> trees against play facts (variable bag, chose, visited).</summary>
public static class StoryWhere
{
    /// <summary>AND two optional trees. Nulls are skipped.</summary>
    public static WhereClause? And(WhereClause? left, WhereClause? right)
    {
        if (left is null)
            return right;
        if (right is null)
            return left;
        return WhereClauseBuilder.CombineAs(GroupOperatorEnum.And, left, right);
    }

    /// <summary>De Morgan invert: And becomes Or of inverted children; comparison operators swap with their negations.</summary>
    public static WhereClause? Negate(WhereClause? clause)
    {
        if (clause is null)
            return null;
        return NegateNode(clause);
    }

    /// <summary>True when <paramref name="clause"/> is null (always) or matches play facts.</summary>
    public static bool Matches(
        WhereClause? clause,
        IReadOnlyDictionary<string, string?> variables,
        ISet<string> chose,
        ISet<string> visited)
    {
        if (clause is null)
            return true;
        return Evaluate(clause, variables, chose, visited);
    }

    private static WhereClause NegateNode(WhereClause clause)
        => clause switch {
            GroupClause group => new GroupClause(
                group.Operator == GroupOperatorEnum.And ? GroupOperatorEnum.Or : GroupOperatorEnum.And,
                group.Children.Select(NegateNode).ToList(),
                group.Description),
            ConditionClause condition => new ConditionClause(condition.Field, NegateComparison(condition.Comparison), condition.Value, condition.Description, condition.Threshold),
            var _ => throw new StoryException($"Unsupported where-clause type '{clause.GetType().Name}'.")
        };

    private static ComparisonOperatorEnum NegateComparison(ComparisonOperatorEnum comparison)
        => comparison switch {
            ComparisonOperatorEnum.Equals => ComparisonOperatorEnum.NotEquals,
            ComparisonOperatorEnum.NotEquals => ComparisonOperatorEnum.Equals,
            ComparisonOperatorEnum.Contains => ComparisonOperatorEnum.NotContains,
            ComparisonOperatorEnum.NotContains => ComparisonOperatorEnum.Contains,
            ComparisonOperatorEnum.StartsWith => ComparisonOperatorEnum.NotStartsWith,
            ComparisonOperatorEnum.NotStartsWith => ComparisonOperatorEnum.StartsWith,
            ComparisonOperatorEnum.EndsWith => ComparisonOperatorEnum.NotEndsWith,
            ComparisonOperatorEnum.NotEndsWith => ComparisonOperatorEnum.EndsWith,
            ComparisonOperatorEnum.GreaterThan => ComparisonOperatorEnum.LessThanOrEqual,
            ComparisonOperatorEnum.GreaterThanOrEqual => ComparisonOperatorEnum.LessThan,
            ComparisonOperatorEnum.LessThan => ComparisonOperatorEnum.GreaterThanOrEqual,
            ComparisonOperatorEnum.LessThanOrEqual => ComparisonOperatorEnum.GreaterThan,
            ComparisonOperatorEnum.In => ComparisonOperatorEnum.NotIn,
            ComparisonOperatorEnum.NotIn => ComparisonOperatorEnum.In,
            var _ => throw new StoryException($"Cannot negate comparison '{comparison}'.")
        };

    private static bool Evaluate(
        WhereClause clause,
        IReadOnlyDictionary<string, string?> variables,
        ISet<string> chose,
        ISet<string> visited)
        => clause switch {
            GroupClause group when group.Operator == GroupOperatorEnum.And => group.Children.All(c => Evaluate(c, variables, chose, visited)),
            GroupClause group => group.Children.Any(c => Evaluate(c, variables, chose, visited)),
            ConditionClause condition => EvaluateCondition(condition, variables, chose, visited),
            var _ => throw new StoryException($"Unsupported where-clause type '{clause.GetType().Name}'.")
        };

    private static bool EvaluateCondition(
        ConditionClause condition,
        IReadOnlyDictionary<string, string?> variables,
        ISet<string> chose,
        ISet<string> visited)
    {
        ArgumentHelpers.ThrowIfNull(condition);
        var field = condition.Field ?? string.Empty;
        string? actual;
        if (field.StartsWith(StoryConditionFields.VarPrefix, StringComparison.Ordinal))
            variables.TryGetValue(field.Substring(StoryConditionFields.VarPrefix.Length), out actual);
        else if (field.StartsWith(StoryConditionFields.ChosePrefix, StringComparison.Ordinal))
            actual = chose.Contains(field.Substring(StoryConditionFields.ChosePrefix.Length)) ? bool.TrueString : bool.FalseString;
        else if (field.StartsWith(StoryConditionFields.VisitedPrefix, StringComparison.Ordinal))
            actual = visited.Contains(field.Substring(StoryConditionFields.VisitedPrefix.Length)) ? bool.TrueString : bool.FalseString;
        else
            throw new StoryException($"Unknown condition field '{field}'. Use {StoryConditionFields.VarPrefix}, {StoryConditionFields.ChosePrefix}, or {StoryConditionFields.VisitedPrefix} prefixes.");

        return Compare(actual, condition.Comparison, condition.Value);
    }

    private static bool Compare(string? actual, ComparisonOperatorEnum comparison, object? expected)
    {
        switch (comparison) {
            case ComparisonOperatorEnum.Equals:
                return ValuesEqual(actual, expected);
            case ComparisonOperatorEnum.NotEquals:
                return !ValuesEqual(actual, expected);
            case ComparisonOperatorEnum.Contains:
                return actual is not null && expected is not null && actual.Contains(Convert.ToString(expected) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotContains:
                return actual is null || expected is null || !actual.Contains(Convert.ToString(expected) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.StartsWith:
                return actual is not null && expected is not null && actual.StartsWith(Convert.ToString(expected) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotStartsWith:
                return actual is null || expected is null || !actual.StartsWith(Convert.ToString(expected) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.EndsWith:
                return actual is not null && expected is not null && actual.EndsWith(Convert.ToString(expected) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotEndsWith:
                return actual is null || expected is null || !actual.EndsWith(Convert.ToString(expected) ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.In:
                return Candidates(expected).Any(candidate => ValuesEqual(actual, candidate));
            case ComparisonOperatorEnum.NotIn:
                return !Candidates(expected).Any(candidate => ValuesEqual(actual, candidate));
            case ComparisonOperatorEnum.GreaterThan:
            case ComparisonOperatorEnum.GreaterThanOrEqual:
            case ComparisonOperatorEnum.LessThan:
            case ComparisonOperatorEnum.LessThanOrEqual:
                if (!TryToDecimal(actual, out var left) || !TryToDecimal(expected, out var right))
                    return false;
                return comparison switch {
                    ComparisonOperatorEnum.GreaterThan => left > right,
                    ComparisonOperatorEnum.GreaterThanOrEqual => left >= right,
                    ComparisonOperatorEnum.LessThan => left < right,
                    var _ => left <= right
                };
            default:
                throw new StoryException($"Unsupported comparison '{comparison}' in story conditions.");
        }
    }

    private static IEnumerable<object?> Candidates(object? expected)
        => expected switch {
            null => [],
            string text => [text],
            JsonElement { ValueKind: JsonValueKind.Array } array => array.EnumerateArray().Select(Scalar),
            JsonElement element => [Scalar(element)],
            System.Collections.IEnumerable items => items.Cast<object?>(),
            var single => [single]
        };

    private static object? Scalar(JsonElement element)
        => element.ValueKind switch {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.String => element.GetString(),
            var _ => element.GetRawText()
        };

    private static bool ValuesEqual(string? actual, object? expected)
    {
        if (expected is bool b)
            return ParseBool(actual) == b;
        if (TryToDecimal(actual, out var left) && TryToDecimal(expected, out var right))
            return left == right;
        var expectedText = expected is null ? null : Convert.ToString(expected);
        if (string.IsNullOrEmpty(actual) && string.IsNullOrEmpty(expectedText))
            return expected is not bool;
        return string.Equals(actual, expectedText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (bool.TryParse(value, out var parsed))
            return parsed;
        return value is "1" or "yes" or "on";
    }

    private static bool TryToDecimal(object? value, out decimal number)
    {
        number = 0;
        if (value is null)
            return false;
        if (value is decimal d) {
            number = d;
            return true;
        }
        if (value is int i) {
            number = i;
            return true;
        }
        if (value is long l) {
            number = l;
            return true;
        }
        return decimal.TryParse(Convert.ToString(value), out number);
    }
}
