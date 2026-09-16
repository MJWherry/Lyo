using Lyo.Exceptions;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Models.Builders;

/// <summary>
/// Builds the <see cref="WhereClause" /> that gates a workflow step. Each predicate reads a context path, so a gate can test an upstream step's result, a value the run was
/// started with, or a field of the workflow itself, and <see cref="All" /> / <see cref="Any" /> nest to express a branch on more than one of them.
/// <para>
/// <see cref="When" /> names the source step rather than its id, because <see cref="JobWorkflowBuilder" /> mints step ids client-side: writing the gate by name is what lets
/// a whole conditional graph be expressed before any of it exists server-side.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // Notify only when the extract found rows and the run was started for production.
/// JobWorkflowStepCondition.All(
///     JobWorkflowStepCondition.When("Extract", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
///     JobWorkflowStepCondition.WhenPath("Input.Env", ComparisonOperatorEnum.Equals, "prod"))
/// </code>
/// </example>
public static class JobWorkflowStepCondition
{
    /// <summary>Prefix of a path that reads another step's run. <see cref="JobWorkflowBuilder.Build" /> validates every name under it against the workflow's steps.</summary>
    public const string StepPathPrefix = "Step.";

    /// <summary>Path segment between a step name and a result key, matching what <c>JobContextBag.WithUpstreamRun</c> seeds.</summary>
    public const string ResultPathSegment = "Result";

    /// <summary>
    /// Tests a named result of another step's run. Reads as the sentence it encodes: <c>When("Extract", "RowCount", GreaterThan, "0")</c>.
    /// </summary>
    /// <param name="sourceStepName">Step whose run result is tested. Must name a step added to the same builder, and must be a dependency of the gated step.</param>
    /// <param name="resultKey">Result key read from that step's run.</param>
    /// <param name="comparator">Comparison applied to the result value.</param>
    /// <param name="value">Value the result is compared against.</param>
    public static WhereClause When(string sourceStepName, string resultKey, ComparisonOperatorEnum comparator, string? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceStepName);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(resultKey);
        return new ConditionClause($"{StepPathPrefix}{sourceStepName}.{ResultPathSegment}.{resultKey}", comparator, value);
    }

    /// <summary>
    /// Tests any context path: <c>Input.Env</c> for a value the run was started with, <c>Workflow.Name</c>, or <c>Step.Extract.Parameter.Path</c>. Use
    /// <see cref="When" /> for the common "result of an upstream step" case.
    /// </summary>
    /// <param name="path">Context path the gate reads.</param>
    /// <param name="comparator">Comparison applied to the resolved value.</param>
    /// <param name="value">Value the resolved value is compared against.</param>
    public static WhereClause WhenPath(string path, ComparisonOperatorEnum comparator, string? value)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        return new ConditionClause(path, comparator, value);
    }

    /// <summary>Opens the gate only when every child does.</summary>
    public static WhereClause All(params WhereClause[] children) => Group(GroupOperatorEnum.And, children);

    /// <summary>Opens the gate when any child does.</summary>
    public static WhereClause Any(params WhereClause[] children) => Group(GroupOperatorEnum.Or, children);

    private static WhereClause Group(GroupOperatorEnum op, WhereClause[] children)
    {
        ArgumentHelpers.ThrowIfNull(children);
        return new GroupClause(op, [.. children]);
    }
}
