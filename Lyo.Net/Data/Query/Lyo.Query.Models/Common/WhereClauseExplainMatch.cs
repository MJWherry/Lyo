using System.Diagnostics;
using System.Text;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>Kind of node in a <see cref="WhereClauseExplainNode"/> tree.</summary>
public enum WhereClauseExplainKind
{
    /// <summary>No clause, or null entity (evaluation did not run).</summary>
    None,

    Condition,

    Group
}

/// <summary>
/// Per-node result of explaining a where clause against an entity: whether this subtree matches, its path in the AST, and optional condition metadata.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhereClauseExplainNode
{
    /// <summary>Whether this subtree (including <see cref="SubClause"/> when present) matches the entity.</summary>
    public bool Passed { get; init; }

    public WhereClauseExplainKind Kind { get; init; }

    /// <summary>Path from the root, e.g. <c>0/2</c> for third child of first group; <c>sub</c> for a <see cref="WhereClause.SubClause"/> chain.</summary>
    public string Path { get; init; } = "";

    public string? Description { get; init; }

    public GroupOperatorEnum? GroupOperator { get; init; }

    public IReadOnlyList<WhereClauseExplainNode>? Children { get; init; }

    public string? Field { get; init; }

    public ComparisonOperatorEnum? Comparison { get; init; }

    public object? FilterValue { get; init; }

    /// <summary>String form of the value(s) read from the entity for this condition's field path (scalar, collection samples, or count).</summary>
    public string? ActualValueSummary { get; init; }

    /// <summary>
    /// When this node is a <see cref="ConditionClause"/> with a <see cref="WhereClause.SubClause"/>, whether the primary field predicate alone passed.
    /// Otherwise null.
    /// </summary>
    public bool? PrimaryPredicatePassed { get; init; }

    public WhereClauseExplainNode? SubClause { get; init; }

    public override string ToString()
    {
        var sb = new StringBuilder(128);
        sb.Append(Kind switch {
            WhereClauseExplainKind.None => "[None",
            WhereClauseExplainKind.Condition => "[Condition",
            WhereClauseExplainKind.Group => "[Group",
            _ => "[?"
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
    /// <summary>Path of the <c>Or</c> group node (same as <see cref="WhereClauseExplainNode.Path"/> on that group).</summary>
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

    /// <summary>Same as <see cref="Root"/>.<see cref="WhereClauseExplainNode.Passed"/>.</summary>
    public bool Passed => Root.Passed;

    public WhereClauseExplainNode Root { get; }

    /// <summary>AST path to the first failing condition or group (depth-first, And/SubClause order), when <see cref="Passed"/> is false.</summary>
    public string? BlockingPath { get; }

    /// <summary>Short explanation of why the clause failed, when <see cref="Passed"/> is false.</summary>
    public string? FailureSummary { get; }

    /// <summary>
    /// When any <c>Or</c> group in the tree failed, one entry per direct branch under each such group (nested <c>Or</c>s produce additional rows).
    /// Empty or null when there are no failed <c>Or</c> nodes or when the overall clause passed.
    /// </summary>
    public IReadOnlyList<ExplainOrBranchOutcome>? OrBranchOutcomes { get; }

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

        if (OrBranchOutcomes is { Count: > 0 } ob) {
            sb.Append(" | OrBranches=").Append(ob.Count);
        }

        sb.Append(" | ").Append(Root.ToString()).Append(']');
        return sb.ToString();
    }
}
