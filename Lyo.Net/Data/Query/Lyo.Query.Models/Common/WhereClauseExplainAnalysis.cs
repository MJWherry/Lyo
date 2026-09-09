using Lyo.Exceptions;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>
/// Post-process of a built <see cref="WhereClauseExplainNode" /> tree: find the blocking failure, phrase it, and list branches of every failed
/// <see cref="GroupOperatorEnum.Or" /> group.
/// </summary>
/// <remarks>
/// Explain work splits in two. Building the node tree needs the evaluated subject — a loaded entity via reflection, or a flat config store by key — so that stays on the
/// evaluator. Describing the finished tree does not, so it lives here and every evaluator yields the same diagnostics from it.
/// </remarks>
public static class WhereClauseExplainAnalysis
{
    /// <summary>Blocking path and failure summary for <paramref name="root" />, or <c>(null, null)</c> when the clause passed.</summary>
    public static (string? BlockingPath, string? FailureSummary) ComputeBlockingFailure(WhereClauseExplainNode root)
    {
        ArgumentHelpers.ThrowIfNull(root);
        return root.Passed ? (null, null) : FindBlockingFailure(root);
    }

    /// <summary>
    /// Depth-first walk of <paramref name="node" /> for the first failure that actually blocks the match, preferring a failed sub-clause when the primary predicate passed.
    /// </summary>
    public static (string? Path, string? Summary) FindBlockingFailure(WhereClauseExplainNode node)
    {
        ArgumentHelpers.ThrowIfNull(node);
        if (node.Passed)
            return (null, null);

        switch (node.Kind) {
            case WhereClauseExplainKind.None:
                return (null, null);
            case WhereClauseExplainKind.Condition:
                if (node.SubClause != null && node.PrimaryPredicatePassed == true)
                    return FindBlockingFailure(node.SubClause);

                return (string.IsNullOrEmpty(node.Path) ? null : node.Path, FormatConditionFailureLine(node));
            case WhereClauseExplainKind.Group:
                if (node.Children != null) {
                    foreach (var child in node.Children) {
                        if (!child.Passed) {
                            var inner = FindBlockingFailure(child);
                            if (inner.Path != null || inner.Summary != null)
                                return inner;
                        }
                    }
                }

                if (node.SubClause != null && !node.SubClause.Passed)
                    return FindBlockingFailure(node.SubClause);

                return (string.IsNullOrEmpty(node.Path) ? null : node.Path, "Group conditions were not satisfied.");
            default:
                return (string.IsNullOrEmpty(node.Path) ? null : node.Path, "Clause did not match.");
        }
    }

    /// <summary>Formats a failed condition node as one diagnostic line, appending the actual value when the evaluator captured one.</summary>
    public static string FormatConditionFailureLine(WhereClauseExplainNode node)
    {
        ArgumentHelpers.ThrowIfNull(node);
        var field = node.Field ?? "?";
        var cmp = node.Comparison?.ToString() ?? "?";
        var actual = node.ActualValueSummary;
        return string.IsNullOrEmpty(actual) ? $"{field} {cmp} is not satisfied." : $"{field} {cmp} is not satisfied (actual: {actual}).";
    }

    /// <summary>Each direct branch under every failed <see cref="GroupOperatorEnum.Or" /> group (nested Or groups included), or <c>null</c> when there are none.</summary>
    public static IReadOnlyList<ExplainOrBranchOutcome>? CollectOrBranchOutcomes(WhereClauseExplainNode root)
    {
        ArgumentHelpers.ThrowIfNull(root);
        var list = new List<ExplainOrBranchOutcome>();
        VisitForFailedOrGroups(root);
        return list.Count == 0 ? null : list;

        void VisitForFailedOrGroups(WhereClauseExplainNode n)
        {
            if (n.Kind == WhereClauseExplainKind.Group && n.GroupOperator == GroupOperatorEnum.Or && !n.Passed && n.Children is { Count: > 0 } orChildren) {
                var orPath = n.Path;
                foreach (var branch in orChildren) {
                    list.Add(
                        new() {
                            OrGroupPath = orPath,
                            BranchPath = branch.Path,
                            Passed = branch.Passed,
                            Summary = SummarizeOrBranchOutcome(branch)
                        });
                }
            }

            if (n.Children != null) {
                foreach (var ch in n.Children)
                    VisitForFailedOrGroups(ch);
            }

            if (n.SubClause != null)
                VisitForFailedOrGroups(n.SubClause);
        }
    }

    private static string SummarizeOrBranchOutcome(WhereClauseExplainNode branch)
    {
        if (branch.Passed)
            return "Branch passed.";

        return branch.Kind switch {
            WhereClauseExplainKind.Condition => FormatConditionFailureLine(branch),
            WhereClauseExplainKind.Group => SummarizeFailedGroupBranch(branch),
            WhereClauseExplainKind.None => "Branch did not match.",
            var _ => "Branch did not match."
        };
    }

    private static string SummarizeFailedGroupBranch(WhereClauseExplainNode group)
    {
        var inner = FindBlockingFailure(group);
        return inner.Summary is { Length: > 0 } summary ? summary : "Subgroup did not match.";
    }
}
