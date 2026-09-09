using System.Diagnostics;
using System.Text;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Models.Common;

/// <summary>Node kind in a <see cref="WhereClauseExplainNode" /> tree.</summary>
public enum WhereClauseExplainKind
{
    /// <summary>No clause, or a null entity (evaluation never ran).</summary>
    None, Condition,
    Group
}

/// <summary>Per-node explain of a where clause against an entity: whether this subtree matches, its AST path, and optional condition metadata.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhereClauseExplainNode
{
    /// <summary>True when this subtree (including <see cref="SubClause" /> when present) matches the entity.</summary>
    public bool Passed { get; init; }

    /// <summary>Condition leaf, group, or none (placeholder).</summary>
    public WhereClauseExplainKind Kind { get; init; }

    /// <summary>Path from the root, e.g. <c>0/2</c> for the third child of the first group; <c>sub</c> for a <see cref="WhereClause.SubClause" /> chain.</summary>
    public string Path { get; init; } = "";

    /// <summary>Optional label copied from the source <see cref="WhereClause" />.</summary>
    public string? Description { get; init; }

    /// <summary>AND or OR on group nodes; otherwise null.</summary>
    public GroupOperatorEnum? GroupOperator { get; init; }

    /// <summary>Child explain nodes in order on group nodes.</summary>
    public IReadOnlyList<WhereClauseExplainNode>? Children { get; init; }

    /// <summary>Dotted field path on condition nodes.</summary>
    public string? Field { get; init; }

    /// <summary>Comparison operator on condition nodes.</summary>
    public ComparisonOperatorEnum? Comparison { get; init; }

    /// <summary>Literal filter value from the clause on condition nodes.</summary>
    public object? FilterValue { get; init; }

    /// <summary>String form of the value(s) read from the entity for this condition's field path (scalar, collection samples, or a count).</summary>
    public string? ActualValueSummary { get; init; }

    /// <summary>When this node is a <see cref="ConditionClause" /> with a <see cref="WhereClause.SubClause" />, whether the primary field predicate alone passed; otherwise null.</summary>
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

/// <summary>One alternative under a failed <c>Or</c> group: whether it passed, plus a one-line explanation.</summary>
public sealed class ExplainOrBranchOutcome
{
    /// <summary>Path of the parent <c>Or</c> group (same as <see cref="WhereClauseExplainNode.Path" /> on that group).</summary>
    public string OrGroupPath { get; init; } = "";

    /// <summary>Path of this branch (direct child of the <c>Or</c> group).</summary>
    public string BranchPath { get; init; } = "";

    public bool Passed { get; init; }

    /// <summary>Short outcome: failure line, success note, or nested blocker summary.</summary>
    public string Summary { get; init; } = "";
}

/// <summary>Result of explaining a where clause against an entity instance.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class WhereClauseExplainResult
{
    /// <summary>Mirrors <see cref="Root" />.<see cref="WhereClauseExplainNode.Passed" />.</summary>
    public bool Passed => Root.Passed;

    /// <summary>Root of the explain tree that mirrors the where-clause structure.</summary>
    public WhereClauseExplainNode Root { get; }

    /// <summary>AST path to the first failing condition or group (depth-first, And/SubClause order) when <see cref="Passed" /> is false.</summary>
    public string? BlockingPath { get; }

    /// <summary>Short reason the clause failed, when <see cref="Passed" /> is false.</summary>
    public string? FailureSummary { get; }

    /// <summary>
    /// When any <c>Or</c> group in the tree failed, one entry per direct branch under each such group (nested <c>Or</c>s add more rows). Empty or null when there are
    /// no failed <c>Or</c> nodes or when the overall clause passed.
    /// </summary>
    public IReadOnlyList<ExplainOrBranchOutcome>? OrBranchOutcomes { get; }

    /// <summary>Builds an explain result with optional blocking path and OR-branch detail.</summary>
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
}
