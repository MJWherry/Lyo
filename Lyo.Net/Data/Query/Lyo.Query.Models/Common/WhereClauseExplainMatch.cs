using System.Diagnostics;
using System.Text;
using Lyo.Query.Models.Enums;
using Lyo.Result;

namespace Lyo.Query.Models.Common;

/// <summary>Kind of node in a <see cref="WhereClauseExplainNode" /> tree.</summary>
public enum WhereClauseExplainKind
{
    /// <summary>No clause, or null entity (evaluation did not run).</summary>
    None, Condition,
    Group
}

/// <summary>Per-node result of explaining a where clause against an entity: whether this subtree matches, its path in the AST, and optional condition metadata.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhereClauseExplainNode
{
    /// <summary>Whether this subtree (including <see cref="SubClause" /> when present) matches the entity.</summary>
    public bool Passed { get; init; }

    /// <summary>Whether this node is a condition leaf, a group, or none (placeholder).</summary>
    public WhereClauseExplainKind Kind { get; init; }

    /// <summary>Path from the root, e.g. <c>0/2</c> for third child of first group; <c>sub</c> for a <see cref="WhereClause.SubClause" /> chain.</summary>
    public string Path { get; init; } = "";

    /// <summary>Optional description copied from the source <see cref="WhereClause" />.</summary>
    public string? Description { get; init; }

    /// <summary>For group nodes, AND or OR; otherwise null.</summary>
    public GroupOperatorEnum? GroupOperator { get; init; }

    /// <summary>For group nodes, child explanation nodes in order.</summary>
    public IReadOnlyList<WhereClauseExplainNode>? Children { get; init; }

    /// <summary>For condition nodes, the dotted field path.</summary>
    public string? Field { get; init; }

    /// <summary>For condition nodes, the comparison operator.</summary>
    public ComparisonOperatorEnum? Comparison { get; init; }

    /// <summary>For condition nodes, the literal filter value from the clause.</summary>
    public object? FilterValue { get; init; }

    /// <summary>String form of the value(s) read from the entity for this condition's field path (scalar, collection samples, or count).</summary>
    public string? ActualValueSummary { get; init; }

    /// <summary>When this node is a <see cref="ConditionClause" /> with a <see cref="WhereClause.SubClause" />, whether the primary field predicate alone passed. Otherwise null.</summary>
    public bool? PrimaryPredicatePassed { get; init; }

    public WhereClauseExplainNode? SubClause { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder(128);
        sb.Append(
            Kind switch {
                WhereClauseExplainKind.None => "[None",
                WhereClauseExplainKind.Condition => "[Condition",
                WhereClauseExplainKind.Group => "[Group",
                var _ => "[?"
            });

        if (Kind == WhereClauseExplainKind.Group && GroupOperator is { } go)
            sb.Append(' ').Append(go);

        sb.Append(Passed ? " pass" : " FAIL");
        if (!string.IsNullOrEmpty(Path))
            sb.Append(" path=").Append(Path);

        switch (Kind) {
            case WhereClauseExplainKind.Condition:
                if (!string.IsNullOrEmpty(Field))
                    sb.Append(' ').Append(Field);

                if (Comparison is { } c)
                    sb.Append(' ').Append(c);

                if (ActualValueSummary != null)
                    sb.Append(" actual=").Append(ActualValueSummary);

                break;
            case WhereClauseExplainKind.Group:
                if (Children is { Count: > 0 } ch)
                    sb.Append(" children=").Append(ch.Count);

                break;
        }

        if (SubClause != null)
            sb.Append(" hasSub");

        sb.Append(']');
        return sb.ToString();
    }
}

/// <summary>One alternative under a failed <c>Or</c> group: whether it passed and a one-line explanation.</summary>
public sealed class ExplainOrBranchOutcome
{
    /// <summary>Path of the <c>Or</c> group node (same as <see cref="WhereClauseExplainNode.Path" /> on that group).</summary>
    public string OrGroupPath { get; init; } = "";

    /// <summary>Path of this branch (direct child of the <c>Or</c>).</summary>
    public string BranchPath { get; init; } = "";

    public bool Passed { get; init; }

    /// <summary>Short outcome: failure line, or success note, or nested blocker summary.</summary>
    public string Summary { get; init; } = "";
}

/// <summary>Outcome of explaining a where clause against an entity instance.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhereClauseExplainResult
{
    /// <summary>Same as <see cref="Root" />.<see cref="WhereClauseExplainNode.Passed" />.</summary>
    public bool Passed => Root.Passed;

    /// <summary>Root of the explanation tree mirroring the where clause structure.</summary>
    public WhereClauseExplainNode Root { get; }

    /// <summary>AST path to the first failing condition or group (depth-first, And/SubClause order), when <see cref="Passed" /> is false.</summary>
    public string? BlockingPath { get; }

    /// <summary>Short explanation of why the clause failed, when <see cref="Passed" /> is false.</summary>
    public string? FailureSummary { get; }

    /// <summary>
    /// When any <c>Or</c> group in the tree failed, one entry per direct branch under each such group (nested <c>Or</c>s produce additional rows). Empty or null when there are
    /// no failed <c>Or</c> nodes or when the overall clause passed.
    /// </summary>
    public IReadOnlyList<ExplainOrBranchOutcome>? OrBranchOutcomes { get; }

    /// <summary>Constructs an explain result with optional blocking path and OR-branch detail.</summary>
    public WhereClauseExplainResult(
        WhereClauseExplainNode root,
        string? blockingPath = null,
        string? failureSummary = null,
        IReadOnlyList<ExplainOrBranchOutcome>? orBranchOutcomes = null)
    {
        Root = root;
        BlockingPath = blockingPath;
        FailureSummary = failureSummary;
        OrBranchOutcomes = orBranchOutcomes;
    }

    public override string ToString()
    {
        var sb = new StringBuilder(160);
        sb.Append("[ExplainResult ").Append(Passed ? "pass" : "FAIL");
        if (!Passed) {
            if (!string.IsNullOrEmpty(BlockingPath))
                sb.Append(" block=").Append(BlockingPath);

            if (!string.IsNullOrEmpty(FailureSummary))
                sb.Append(" - ").Append(FailureSummary);
        }

        if (OrBranchOutcomes is { Count: > 0 } ob)
            sb.Append(" | OrBranches=").Append(ob.Count);

        sb.Append(" | ").Append(Root).Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Maps a failed in-memory explain tree to structured <see cref="Error" />s. AND groups emit one error per failing condition leaf; OR groups emit a single error from
    /// <see cref="FailureSummary" /> (or the group description). Does not apply to SQL <c>ApplyWhereClause</c> — queries exclude rows instead of producing errors.
    /// </summary>
    /// <param name="messages">Optional code/message overrides keyed by dotted field path (ordinal ignore-case).</param>
    /// <returns>An empty list when <see cref="Passed" /> is true.</returns>
    public IReadOnlyList<Error> ToErrors(IReadOnlyDictionary<string, WhereClauseErrorOverride>? messages = null)
    {
        if (Passed)
            return [];

        var errors = new List<Error>();
        if (Root.Kind == WhereClauseExplainKind.None) {
            errors.Add(CreateError(field: null, comparison: null, actualValue: null, filterValue: null, description: null, fallbackMessage: FailureSummary ?? "Where clause did not match.", messages));
            return errors;
        }

        CollectErrors(Root, errors, messages);
        if (errors.Count == 0)
            errors.Add(CreateError(field: null, comparison: null, actualValue: null, filterValue: null, description: null, fallbackMessage: FailureSummary ?? "Where clause did not match.", messages));

        return errors;
    }

    private void CollectErrors(WhereClauseExplainNode node, List<Error> errors, IReadOnlyDictionary<string, WhereClauseErrorOverride>? messages)
    {
        if (node.Passed)
            return;

        if (node.Kind == WhereClauseExplainKind.Group && node.GroupOperator == GroupOperatorEnum.Or) {
            var field = FirstFailingField(node);
            errors.Add(CreateError(field, comparison: null, actualValue: null, filterValue: null, node.Description, FailureSummary ?? node.ToString(), messages));
            return;
        }

        if (node.Kind == WhereClauseExplainKind.Group) {
            if (node.Children != null) {
                foreach (var child in node.Children)
                    CollectErrors(child, errors, messages);
            }

            if (node.SubClause is { Passed: false } groupSub)
                CollectErrors(groupSub, errors, messages);

            return;
        }

        if (node.Kind == WhereClauseExplainKind.Condition) {
            if (node.SubClause is { Passed: false } sub && node.PrimaryPredicatePassed == true) {
                CollectErrors(sub, errors, messages);
                return;
            }

            errors.Add(CreateError(node.Field, node.Comparison, node.ActualValueSummary, node.FilterValue, node.Description, FailureSummary, messages));
            return;
        }

        if (node.SubClause is { Passed: false } leftover)
            CollectErrors(leftover, errors, messages);
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

        if (value is System.Collections.IEnumerable many and not string) {
            var items = many.Cast<object?>().Select(x => x?.ToString() ?? "NULL");
            return $"[{string.Join(", ", items)}]";
        }

        return value.ToString() ?? "NULL";
    }
}