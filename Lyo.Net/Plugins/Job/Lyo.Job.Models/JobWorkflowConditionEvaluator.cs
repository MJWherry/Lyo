using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Models;

/// <summary>
/// Decides whether a conditional workflow step runs, by testing the step's <see cref="WhereClause" /> against its <see cref="JobContextBag" />. Pure and shared so the
/// engine and any preview UI reach the same verdict.
/// <para>
/// Each <c>ConditionClause.Field</c> is a context path rather than an entity property, so a gate can read an upstream step's result
/// (<c>Step.Extract.Result.RowCount</c>), a workflow input (<c>Input.Env</c>), or a field of the workflow or definition (<c>Workflow.Name</c>). A <c>GroupClause</c>
/// combines predicates with AND/OR and nests, which is what lets one workflow branch on more than a single value.
/// </para>
/// <para>
/// Every way of failing to evaluate — an unparseable clause, a path nothing seeded, an unsupported comparator — returns <see cref="JobWorkflowConditionOutcome.Skip" />. A
/// condition exists to stop the step in some circumstance, so guessing "run" when the gate cannot be read would defeat it in exactly the cases the author cared about.
/// </para>
/// </summary>
public static class JobWorkflowConditionEvaluator
{
    private const BindingFlags PropertyFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    /// <summary>Evaluates <paramref name="step" />'s condition. A step with no <see cref="JobWorkflowStepRes.ConditionJson" /> is unconditional and always runs.</summary>
    /// <param name="step">Step whose gate is being tested.</param>
    /// <param name="context">Context the gate reads. Supply the step's own scope, so <c>Step.{name}</c> paths resolve to its dependencies' runs.</param>
    public static JobWorkflowConditionOutcome Evaluate(JobWorkflowStepRes step, JobContextBag context)
    {
        if (step is null || string.IsNullOrWhiteSpace(step.ConditionJson))
            return JobWorkflowConditionOutcome.Run;

        var clause = TryParse(step.ConditionJson!);
        if (clause is null)
            return JobWorkflowConditionOutcome.Skip;

        return Matches(clause, context ?? new JobContextBag()) ? JobWorkflowConditionOutcome.Run : JobWorkflowConditionOutcome.Skip;
    }

    /// <summary>Reads a serialized clause, or null when it cannot be parsed. A clause written by an older or hand-edited client must not take the engine down.</summary>
    public static WhereClause? TryParse(string? conditionJson)
    {
        if (string.IsNullOrWhiteSpace(conditionJson))
            return null;

        try {
            return JsonSerializer.Deserialize<WhereClause>(conditionJson!);
        }
        catch (JsonException) {
            return null;
        }
        catch (NotSupportedException) {
            return null;
        }
    }

    private static bool Matches(WhereClause clause, JobContextBag context)
    {
        var matched = clause switch {
            GroupClause group => MatchesGroup(group, context),
            ConditionClause condition => MatchesCondition(condition, context),
            var _ => false
        };

        // SubClause is AND-chained after its owner wherever the query engine evaluates a clause; a gate has to read the same way or the same JSON means two things.
        return matched && (clause.SubClause is null || Matches(clause.SubClause, context));
    }

    /// <summary>An empty group states nothing, so it gates nothing. Skipping there would strand every step whose clause was cleared in the editor.</summary>
    private static bool MatchesGroup(GroupClause group, JobContextBag context)
        => group.Children.Count == 0
           || (group.Operator == GroupOperatorEnum.Or ? group.Children.Any(c => Matches(c, context)) : group.Children.All(c => Matches(c, context)));

    private static bool MatchesCondition(ConditionClause condition, JobContextBag context)
        => TryResolve(context, condition.Field, out var actual) && Matches(actual, condition.Comparison, Text(condition.Value));

    /// <summary>
    /// Reads a context path. Tried as a flat key first because <see cref="JobContextBag" /> writes its nested seeds under dotted keys
    /// (<c>Step.Extract.Result.RowCount</c>), then as a bag/dictionary path, and finally by walking object properties — without that last step the object-valued entries
    /// (<c>Workflow</c>, <c>Definition</c>) would have no readable fields at all.
    /// </summary>
    private static bool TryResolve(JobContextBag context, string field, out string? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(field))
            return false;

        if (context.TryGetValue(field, out var direct)) {
            value = Text(direct);
            return true;
        }

        if (context.TryGetPath(field, out var viaPath)) {
            value = Text(viaPath);
            return true;
        }

        return TryResolveByProperty(context, field, out value);
    }

    /// <summary>Longest seeded prefix wins, so <c>Workflow.JobWorkflow.Name</c> still resolves when both <c>Workflow</c> and a longer key are present.</summary>
    private static bool TryResolveByProperty(JobContextBag context, string field, out string? value)
    {
        value = null;
        var segments = field.Split('.');
        for (var take = segments.Length - 1; take > 0; take--) {
            if (!context.TryGetValue(string.Join(".", segments.Take(take)), out var root) || root is null)
                continue;

            if (!TryWalk(root, segments.Skip(take), out var walked))
                continue;

            value = Text(walked);
            return true;
        }

        return false;
    }

    private static bool TryWalk(object root, IEnumerable<string> segments, out object? result)
    {
        var current = root;
        foreach (var segment in segments) {
            if (current is null) {
                result = null;
                return false;
            }

            var property = current.GetType().GetProperty(segment, PropertyFlags);
            if (property is null || !property.CanRead || property.GetIndexParameters().Length > 0) {
                result = null;
                return false;
            }

            try {
                current = property.GetValue(current);
            }
            catch (TargetInvocationException) {
                result = null;
                return false;
            }
        }

        result = current;
        return true;
    }

    /// <summary>Renders a clause value or a resolved context value as the text the comparators work on. Lists become CSV, which is the form In and NotIn already read.</summary>
    private static string? Text(object? value)
        => value switch {
            null => null,
            string text => text,
            IEnumerable many => string.Join(",", many.Cast<object?>().Select(v => Text(v) ?? "")),
            var other => Convert.ToString(other, CultureInfo.InvariantCulture)
        };

    /// <summary>
    /// Applies one comparison. Ordering comparators compare numerically when both sides parse as numbers, otherwise ordinally — without that, a row-count gate of
    /// <c>&gt; 100</c> would let <c>90</c> through, because "90" sorts after "100" as text.
    /// </summary>
    private static bool Matches(string? actual, ComparisonOperatorEnum comparator, string? expected)
    {
        var left = actual ?? "";
        var right = expected ?? "";
        switch (comparator) {
            case ComparisonOperatorEnum.Equals:
            case ComparisonOperatorEnum.Unknown:
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotEquals:
                return !string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.Contains:
                return left.IndexOf(right, StringComparison.OrdinalIgnoreCase) >= 0;
            case ComparisonOperatorEnum.NotContains:
                return left.IndexOf(right, StringComparison.OrdinalIgnoreCase) < 0;
            case ComparisonOperatorEnum.StartsWith:
                return left.StartsWith(right, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotStartsWith:
                return !left.StartsWith(right, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.EndsWith:
                return left.EndsWith(right, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotEndsWith:
                return !left.EndsWith(right, StringComparison.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.GreaterThan:
                return Compare(left, right) > 0;
            case ComparisonOperatorEnum.GreaterThanOrEqual:
                return Compare(left, right) >= 0;
            case ComparisonOperatorEnum.LessThan:
                return Compare(left, right) < 0;
            case ComparisonOperatorEnum.LessThanOrEqual:
                return Compare(left, right) <= 0;
            case ComparisonOperatorEnum.In:
                return SplitList(right).Contains(left, StringComparer.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.NotIn:
                return !SplitList(right).Contains(left, StringComparer.OrdinalIgnoreCase);
            case ComparisonOperatorEnum.Regex:
                return IsRegexMatch(left, right);
            case ComparisonOperatorEnum.NotRegex:
                return !IsRegexMatch(left, right);
            default:
                // Fuzzy and NotFuzzy need the database's trigram support; there is no honest in-process answer.
                return false;
        }
    }

    private static int Compare(string left, string right)
    {
        if (decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var leftNumber)
            && decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        if (DateTimeOffset.TryParse(left, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var leftDate)
            && DateTimeOffset.TryParse(right, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var rightDate))
            return leftDate.CompareTo(rightDate);

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] SplitList(string value) => value.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToArray();

    /// <summary>Runs an author-supplied pattern under a timeout. A bad pattern reads as "did not match" rather than taking the workflow engine down.</summary>
    private static bool IsRegexMatch(string input, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        try {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch (ArgumentException) {
            return false;
        }
        catch (RegexMatchTimeoutException) {
            return false;
        }
    }
}
