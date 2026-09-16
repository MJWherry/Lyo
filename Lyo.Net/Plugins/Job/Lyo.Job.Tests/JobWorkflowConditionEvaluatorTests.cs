using System.Text.Json;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Job.Models;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Tests;

/// <summary>
/// A conditional step is how a workflow branches: "email the on-call rota, but only when the extract found rows". These lock the comparator semantics, the AND/OR nesting,
/// and the ways a condition can fail to evaluate — an unreadable path or unparseable clause — all of which must skip rather than run the step on a guess.
/// </summary>
public class JobWorkflowConditionEvaluatorTests
{
    [Fact]
    public void Evaluate_SinglePredicateOnUpstreamResult_ReturnsRun()
    {
        var context = new JobContextBag().WithUpstreamRun("Extract", RunWith(("RowCount", "42")));
        var step = BuildStep(Condition("Step.Extract.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "0"));

        Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_AndGroup_OneChildFails_ReturnsSkip()
    {
        var context = new JobContextBag().WithUpstreamRun("Extract", RunWith(("RowCount", "42"), ("Status", "failed")));
        var step = BuildStep(
            Group(
                GroupOperatorEnum.And,
                Condition("Step.Extract.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
                Condition("Step.Extract.Result.Status", ComparisonOperatorEnum.Equals, "ok")));

        Assert.Equal(JobWorkflowConditionOutcome.Skip, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_OrGroup_OneChildMatches_ReturnsRun()
    {
        var context = new JobContextBag().WithUpstreamRun("Extract", RunWith(("RowCount", "0"), ("Status", "ok")));
        var step = BuildStep(
            Group(
                GroupOperatorEnum.Or,
                Condition("Step.Extract.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
                Condition("Step.Extract.Result.Status", ComparisonOperatorEnum.Equals, "ok")));

        Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_NestedGroups_InnerOrInsideOuterAnd_ReturnsRun()
    {
        var context = new JobContextBag().WithUpstreamRun("Extract", RunWith(("RowCount", "42"), ("Status", "warn")));
        var step = BuildStep(
            Group(
                GroupOperatorEnum.And,
                Condition("Step.Extract.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
                Group(
                    GroupOperatorEnum.Or,
                    Condition("Step.Extract.Result.Status", ComparisonOperatorEnum.Equals, "ok"),
                    Condition("Step.Extract.Result.Status", ComparisonOperatorEnum.Equals, "warn"))));

        Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_NestedGroups_InnerOrAllFail_ReturnsSkip()
    {
        var context = new JobContextBag().WithUpstreamRun("Extract", RunWith(("RowCount", "42"), ("Status", "failed")));
        var step = BuildStep(
            Group(
                GroupOperatorEnum.And,
                Condition("Step.Extract.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
                Group(
                    GroupOperatorEnum.Or,
                    Condition("Step.Extract.Result.Status", ComparisonOperatorEnum.Equals, "ok"),
                    Condition("Step.Extract.Result.Status", ComparisonOperatorEnum.Equals, "warn"))));

        Assert.Equal(JobWorkflowConditionOutcome.Skip, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_PathOnRunInput_ReturnsRun()
    {
        var context = new JobContextBag();
        context.Set("Input.Env", "prod");
        var step = BuildStep(Condition("Input.Env", ComparisonOperatorEnum.Equals, "prod"));

        Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_PathOnObjectProperty_ReturnsRun()
    {
        // Workflow is seeded as a JobWorkflowRes, not a nested bag, so this only resolves if the field walk reaches into object properties.
        var workflow = new JobWorkflowRes(LyoGuid.CreateV7(), "Nightly ETL", null, true, null);
        var context = new JobContextBag().WithWorkflow(workflow, null);
        var step = BuildStep(Condition("Workflow.Name", ComparisonOperatorEnum.Equals, "Nightly ETL"));

        Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_UnresolvablePath_ReturnsSkip()
    {
        // Nothing seeded the path, so the gate cannot be read. Running the step would ignore the condition in exactly the case its author cared about.
        var context = new JobContextBag().WithUpstreamRun("Extract", RunWith(("RowCount", "42")));
        var step = BuildStep(Condition("Step.Missing.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "0"));

        Assert.Equal(JobWorkflowConditionOutcome.Skip, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_MalformedConditionJson_ReturnsSkip()
    {
        var step = BuildStepJson("{ not json");
        Assert.Equal(JobWorkflowConditionOutcome.Skip, JobWorkflowConditionEvaluator.Evaluate(step, new()));
    }

    [Fact]
    public void Evaluate_NoConditionJson_ReturnsRun()
        => Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(BuildStepJson(null), new()));

    [Fact]
    public void Evaluate_EmptyGroup_ReturnsRun()
    {
        // A group with no children states nothing, so it gates nothing. Skipping here would strand any step whose clause was cleared in the editor.
        var step = BuildStep(Group(GroupOperatorEnum.And));
        Assert.Equal(JobWorkflowConditionOutcome.Run, JobWorkflowConditionEvaluator.Evaluate(step, new()));
    }

    [Theory]
    [InlineData(ComparisonOperatorEnum.Equals, "Status", "ok", "ok", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.Equals, "Status", "ok", "degraded", JobWorkflowConditionOutcome.Skip)]
    [InlineData(ComparisonOperatorEnum.NotEquals, "Status", "bad", "ok", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.Contains, "Message", "disk", "low disk space", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.NotContains, "Message", "disk", "low disk space", JobWorkflowConditionOutcome.Skip)]
    [InlineData(ComparisonOperatorEnum.StartsWith, "Message", "low", "low disk space", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.EndsWith, "Message", "space", "low disk space", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.GreaterThanOrEqual, "RowCount", "100", "100", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.LessThan, "RowCount", "100", "99", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.LessThanOrEqual, "RowCount", "100", "101", JobWorkflowConditionOutcome.Skip)]
    [InlineData(ComparisonOperatorEnum.In, "Status", "ok,warn", "warn", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.NotIn, "Status", "ok,warn", "warn", JobWorkflowConditionOutcome.Skip)]
    [InlineData(ComparisonOperatorEnum.Regex, "Message", "^low", "low disk space", JobWorkflowConditionOutcome.Run)]
    [InlineData(ComparisonOperatorEnum.NotRegex, "Message", "^high", "low disk space", JobWorkflowConditionOutcome.Run)]
    public void Evaluate_Comparator_MatchesExpectedOutcome(
        ComparisonOperatorEnum comparator,
        string key,
        string conditionValue,
        string resultValue,
        JobWorkflowConditionOutcome expected)
    {
        var context = new JobContextBag().WithUpstreamRun("Source", RunWith((key, resultValue)));
        var step = BuildStep(Condition($"Step.Source.Result.{key}", comparator, conditionValue));

        Assert.Equal(expected, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_GreaterThanOnNumericResult_ComparesNumerically()
    {
        // Ordinal string comparison would call "90" greater than "100". Numbers have to compare as numbers or every row-count gate is subtly wrong.
        var context = new JobContextBag().WithUpstreamRun("Source", RunWith(("RowCount", "90")));
        var step = BuildStep(Condition("Step.Source.Result.RowCount", ComparisonOperatorEnum.GreaterThan, "100"));

        Assert.Equal(JobWorkflowConditionOutcome.Skip, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    [Fact]
    public void Evaluate_UnsupportedComparator_ReturnsSkip()
    {
        // Fuzzy needs the database's trigram support; there is no honest in-process answer, so the safe reading of the gate is "do not run".
        var context = new JobContextBag().WithUpstreamRun("Source", RunWith(("Status", "ok")));
        var step = BuildStep(Condition("Step.Source.Result.Status", ComparisonOperatorEnum.Fuzzy, "ok"));

        Assert.Equal(JobWorkflowConditionOutcome.Skip, JobWorkflowConditionEvaluator.Evaluate(step, context));
    }

    private static ConditionClause Condition(string field, ComparisonOperatorEnum comparison, object? value) => new(field, comparison, value);

    private static GroupClause Group(GroupOperatorEnum op, params WhereClause[] children) => new(op, null, children);

    private static JobWorkflowStepRes BuildStep(WhereClause clause) => BuildStepJson(JsonSerializer.Serialize(clause));

    private static JobWorkflowStepRes BuildStepJson(string? conditionJson)
        => new(LyoGuid.CreateV7(), LyoGuid.CreateV7(), LyoGuid.CreateV7(), "Step", 0, null, JobWorkflowFailurePolicy.Stop, null, true, ConditionJson: conditionJson);

    private static JobRunRes RunWith(params (string Key, string Value)[] results)
    {
        var runId = LyoGuid.CreateV7();
        return new() {
            Id = runId,
            JobDefinitionId = LyoGuid.CreateV7(),
            State = JobState.Finished,
            Result = JobRunResult.Success,
            CreatedTimestamp = DateTime.UtcNow,
            JobRunResults = [.. results.Select(r => new JobRunResultRes(LyoGuid.CreateV7(), runId, r.Key, LyoTypeInfo.String.FullName, r.Value))]
        };
    }
}
