using System.Collections;
using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Result;

namespace Lyo.Validation.Models;

/// <summary>Maps a where-clause explain tree onto structured <see cref="Error" /> values.</summary>
/// <remarks>
/// Kept outside <c>Lyo.Query.Models</c> so query description does not pull in the result stack. Only validation turns a failed match into errors; a
/// SQL <c>ApplyWhereClause</c> drops rows instead.
/// </remarks>
public static class WhereClauseExplainResultExtensions
{
    /// <summary>
    /// Maps a failed in-memory explain tree to structured <see cref="Error" />s. AND groups emit one error per failing condition leaf; OR groups emit one error from
    /// <see cref="WhereClauseExplainResult.FailureSummary" /> (or the group description). Not used for SQL <c>ApplyWhereClause</c> — those queries drop rows rather than
    /// emitting errors.
    /// </summary>
    /// <param name="result">Explain tree to project.</param>
    /// <param name="messages">Optional code/message overrides keyed by dotted field path (ordinal ignore-case).</param>
    /// <returns>Empty list when <see cref="WhereClauseExplainResult.Passed" /> is true.</returns>
    public static IReadOnlyList<Error> ToErrors(this WhereClauseExplainResult result, IReadOnlyDictionary<string, WhereClauseErrorOverride>? messages = null)
    {
        ArgumentHelpers.ThrowIfNull(result);
        if (result.Passed)
            return [];

        var errors = new List<Error>();
        if (result.Root.Kind == WhereClauseExplainKind.None) {
            errors.Add(
                CreateError(
                    field: null, comparison: null, actualValue: null, filterValue: null, description: null,
                    fallbackMessage: result.FailureSummary ?? "Where clause did not match.", messages));

            return errors;
        }

        CollectErrors(result, result.Root, errors, messages);
        if (errors.Count == 0) {
            errors.Add(
                CreateError(
                    field: null, comparison: null, actualValue: null, filterValue: null, description: null,
                    fallbackMessage: result.FailureSummary ?? "Where clause did not match.", messages));
        }

        return errors;
    }

    private static void CollectErrors(
        WhereClauseExplainResult result,
        WhereClauseExplainNode node,
        List<Error> errors,
        IReadOnlyDictionary<string, WhereClauseErrorOverride>? messages)
    {
        if (node.Passed)
            return;

        if (node.Kind == WhereClauseExplainKind.Group && node.GroupOperator == GroupOperatorEnum.Or) {
            var field = FirstFailingField(node);
            errors.Add(CreateError(field, comparison: null, actualValue: null, filterValue: null, node.Description, result.FailureSummary ?? node.ToString(), messages));
            return;
        }

        if (node.Kind == WhereClauseExplainKind.Group) {
            if (node.Children != null) {
                foreach (var child in node.Children)
                    CollectErrors(result, child, errors, messages);
            }

            if (node.SubClause is { Passed: false } groupSub)
                CollectErrors(result, groupSub, errors, messages);

            return;
        }

        if (node.Kind == WhereClauseExplainKind.Condition) {
            if (node.SubClause is { Passed: false } sub && node.PrimaryPredicatePassed == true) {
                CollectErrors(result, sub, errors, messages);
                return;
            }

            errors.Add(CreateError(node.Field, node.Comparison, node.ActualValueSummary, node.FilterValue, node.Description, result.FailureSummary, messages));
            return;
        }

        if (node.SubClause is { Passed: false } leftover)
            CollectErrors(result, leftover, errors, messages);
    }

    private static string? FirstFailingField(WhereClauseExplainNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Field))
            return node.Field;

        if (node.Children == null)
            return node.SubClause is { Passed: false } sub ? FirstFailingField(sub) : null;

        foreach (var child in node.Children) {
            if (child.Passed)
                continue;

            var field = FirstFailingField(child);
            if (!string.IsNullOrWhiteSpace(field))
                return field;
        }

        return node.SubClause is { Passed: false } nested ? FirstFailingField(nested) : null;
    }

    private static Error CreateError(
        string? field,
        ComparisonOperatorEnum? comparison,
        string? actualValue,
        object? filterValue,
        string? description,
        string? fallbackMessage,
        IReadOnlyDictionary<string, WhereClauseErrorOverride>? messages)
    {
        WhereClauseErrorOverride? overrideMessage = null;
        if (field != null && messages != null) {
            foreach (var kvp in messages) {
                if (string.Equals(kvp.Key, field, StringComparison.OrdinalIgnoreCase)) {
                    overrideMessage = kvp.Value;
                    break;
                }
            }
        }

        var message = overrideMessage?.ErrorMessage;
        if (string.IsNullOrWhiteSpace(message))
            message = description;
        if (string.IsNullOrWhiteSpace(message))
            message = BuildDefaultMessage(field, comparison, filterValue, actualValue, fallbackMessage);

        var code = overrideMessage?.ErrorCode;
        if (string.IsNullOrWhiteSpace(code))
            code = DefaultErrorCode(comparison);

        var metadata = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(field))
            metadata["propertyName"] = field!;
        if (actualValue != null)
            metadata["attemptedValue"] = actualValue;
        else if (filterValue != null)
            metadata["attemptedValue"] = filterValue;

        return Error.Validation(message!, code!, metadata);
    }

    private static string DefaultErrorCode(ComparisonOperatorEnum? comparison)
        => comparison switch {
            ComparisonOperatorEnum.Regex or ComparisonOperatorEnum.NotRegex => ValidationErrorCodes.InvalidFormat,
            ComparisonOperatorEnum.In => ValidationErrorCodes.MissingItem,
            ComparisonOperatorEnum.NotIn => ValidationErrorCodes.DisallowedItem,
            var _ => ValidationErrorCodes.ValidationFailed
        };

    private static string BuildDefaultMessage(string? field, ComparisonOperatorEnum? comparison, object? filterValue, string? actualValue, string? fallbackMessage)
    {
        if (!string.IsNullOrWhiteSpace(fallbackMessage) && string.IsNullOrWhiteSpace(field))
            return fallbackMessage!;

        if (!string.IsNullOrWhiteSpace(field) && comparison is { } op)
            return actualValue != null ? $"{field} {op} failed (actual '{actualValue}')." : $"{field} {op} {FormatFilterValue(filterValue)}";

        return fallbackMessage ?? "Where clause did not match.";
    }

    private static string FormatFilterValue(object? value)
    {
        if (value is null)
            return "NULL";

        if (value is string text)
            return $"'{text}'";

        if (value is IEnumerable many and not string) {
            var items = many.Cast<object?>().Select(x => x?.ToString() ?? "NULL");
            return $"[{string.Join(", ", items)}]";
        }

        return value.ToString() ?? "NULL";
    }
}
