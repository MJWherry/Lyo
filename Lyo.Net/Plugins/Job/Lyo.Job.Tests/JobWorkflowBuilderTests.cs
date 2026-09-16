using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.Job.Models;
using Lyo.Job.Models.Builders;
using Lyo.Job.Models.Request;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;

namespace Lyo.Job.Tests;

/// <summary>
/// Authoring a DAG used to take three round trips: create the workflow, read back the server-minted step ids, then PATCH each step's <c>DependsOnStepIds</c>. The builder
/// mints ids client-side and resolves dependencies by step name, so one POST expresses the whole graph — and a typo fails here rather than as a dead workflow run later.
/// </summary>
public class JobWorkflowBuilderTests
{
    [Fact]
    public void AddStep_DependsOnByName_ResolvesToStepId()
    {
        var req = JobWorkflowBuilder.New("etl").AddStep("Extract", LyoGuid.CreateV7()).AddStep("Load", LyoGuid.CreateV7(), dependsOn: ["Extract"]).Build();

        var extract = req.CreateSteps.Single(s => s.StepName == "Extract");
        var load = req.CreateSteps.Single(s => s.StepName == "Load");
        Assert.NotEqual(Guid.Empty, extract.Id);
        Assert.Equal(extract.Id.ToString(), load.DependsOnStepIds);
    }

    [Fact]
    public void AddStep_MultipleDependencies_JoinsIdsWithCommas()
    {
        var req = JobWorkflowBuilder.New("fan-in")
            .AddStep("A", LyoGuid.CreateV7())
            .AddStep("B", LyoGuid.CreateV7())
            .AddStep("C", LyoGuid.CreateV7(), dependsOn: ["A", "B"])
            .Build();

        var expected = $"{StepId(req, "A")},{StepId(req, "B")}";
        Assert.Equal(expected, req.CreateSteps.Single(s => s.StepName == "C").DependsOnStepIds);
    }

    [Fact]
    public void AddStep_UnknownDependencyName_Throws()
    {
        var builder = JobWorkflowBuilder.New("etl").AddStep("Load", LyoGuid.CreateV7(), dependsOn: ["Extarct"]);

        // A typo has to fail at build time. Left alone it becomes a DependsOnStepIds nobody can satisfy, and the run fails as a malformed graph in production.
        var ex = Assert.Throws<ValidationException>(builder.Build);
        Assert.Contains("Extarct", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddStep_DuplicateStepName_Throws()
    {
        var builder = JobWorkflowBuilder.New("etl").AddStep("Load", LyoGuid.CreateV7());
        var ex = Assert.Throws<ValidationException>(() => builder.AddStep("Load", LyoGuid.CreateV7()));
        Assert.Contains("Load", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_StepOrderNotSupplied_DefaultsToInsertionOrder()
    {
        var req = JobWorkflowBuilder.New("etl").AddStep("A", LyoGuid.CreateV7()).AddStep("B", LyoGuid.CreateV7()).AddStep("C", LyoGuid.CreateV7()).Build();

        Assert.Equal([0, 1, 2], req.CreateSteps.Select(s => s.StepOrder).ToArray());
    }

    [Fact]
    public void Build_ExplicitStepOrder_IsPreserved()
    {
        var req = JobWorkflowBuilder.New("etl").AddStep("A", LyoGuid.CreateV7(), stepOrder: 10).AddStep("B", LyoGuid.CreateV7()).Build();

        Assert.Equal(10, req.CreateSteps.Single(s => s.StepName == "A").StepOrder);
        Assert.Equal(1, req.CreateSteps.Single(s => s.StepName == "B").StepOrder);
    }

    [Fact]
    public void Build_NoEnabledSteps_Throws()
    {
        var builder = JobWorkflowBuilder.New("etl").AddStep("A", LyoGuid.CreateV7(), enabled: false);

        // StartWorkflowAsync rejects this workflow anyway; catching it at authoring time beats persisting something that can never run.
        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void AddStep_ConditionWhen_SerializesASingleConditionClauseOnTheStepResultPath()
    {
        var req = JobWorkflowBuilder.New("etl")
            .AddStep("Extract", LyoGuid.CreateV7())
            .AddStep("Notify", LyoGuid.CreateV7(), dependsOn: ["Extract"], condition: JobWorkflowStepCondition.When("Extract", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"))
            .Build();

        var clause = Assert.IsType<ConditionClause>(Parse(req, "Notify"));
        Assert.Equal("Step.Extract.Result.RowCount", clause.Field);
        Assert.Equal(ComparisonOperatorEnum.GreaterThan, clause.Comparison);
        Assert.Equal("0", clause.Value?.ToString());
    }

    [Fact]
    public void AddStep_ConditionAll_SerializesAnAndGroup()
    {
        var condition = JobWorkflowStepCondition.All(
            JobWorkflowStepCondition.When("Extract", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"),
            JobWorkflowStepCondition.WhenPath("Input.Env", ComparisonOperatorEnum.Equals, "prod"));

        var req = JobWorkflowBuilder.New("etl")
            .AddStep("Extract", LyoGuid.CreateV7())
            .AddStep("Notify", LyoGuid.CreateV7(), dependsOn: ["Extract"], condition: condition)
            .Build();

        var group = Assert.IsType<GroupClause>(Parse(req, "Notify"));
        Assert.Equal(GroupOperatorEnum.And, group.Operator);
        Assert.Equal(2, group.Children.Count);
    }

    [Fact]
    public void AddStep_ConditionAny_SerializesAnOrGroup()
    {
        var condition = JobWorkflowStepCondition.Any(
            JobWorkflowStepCondition.When("Extract", "Status", ComparisonOperatorEnum.Equals, "ok"),
            JobWorkflowStepCondition.When("Extract", "Status", ComparisonOperatorEnum.Equals, "warn"));

        var req = JobWorkflowBuilder.New("etl")
            .AddStep("Extract", LyoGuid.CreateV7())
            .AddStep("Notify", LyoGuid.CreateV7(), dependsOn: ["Extract"], condition: condition)
            .Build();

        var group = Assert.IsType<GroupClause>(Parse(req, "Notify"));
        Assert.Equal(GroupOperatorEnum.Or, group.Operator);
        Assert.Equal(2, group.Children.Count);
    }

    [Fact]
    public void Build_ConditionReferencesUnknownStep_Throws()
    {
        var builder = JobWorkflowBuilder.New("etl")
            .AddStep("Notify", LyoGuid.CreateV7(), condition: JobWorkflowStepCondition.When("Nope", "RowCount", ComparisonOperatorEnum.Equals, "1"));

        Assert.Throws<ValidationException>(builder.Build);
    }

    [Fact]
    public void Build_ConditionReferencesNonDependencyStep_Throws()
    {
        // The engine evaluates a gate as soon as the step's own dependencies are satisfied. Reading a step it does not wait for would skip on a run that has not happened yet.
        var builder = JobWorkflowBuilder.New("etl")
            .AddStep("Extract", LyoGuid.CreateV7())
            .AddStep("Notify", LyoGuid.CreateV7(), condition: JobWorkflowStepCondition.When("Extract", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"));

        var ex = Assert.Throws<ValidationException>(builder.Build);
        Assert.Contains("Extract", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Notify", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ConditionOnInputPath_DoesNotRequireADependency()
    {
        var req = JobWorkflowBuilder.New("etl")
            .AddStep("Notify", LyoGuid.CreateV7(), condition: JobWorkflowStepCondition.WhenPath("Input.Env", ComparisonOperatorEnum.Equals, "prod"))
            .Build();

        var clause = Assert.IsType<ConditionClause>(Parse(req, "Notify"));
        Assert.Equal("Input.Env", clause.Field);
    }

    private static WhereClause? Parse(JobWorkflowReq req, string stepName)
        => JobWorkflowConditionEvaluator.TryParse(req.CreateSteps.Single(s => s.StepName == stepName).ConditionJson);

    [Fact]
    public void AddStep_WithParameters_SerializesObjectMapJson()
    {
        var req = JobWorkflowBuilder.New("etl").AddStep("Load", LyoGuid.CreateV7(), parameters: new Dictionary<string, string> { ["Path"] = "/tmp/b" }).Build();

        // The phase 3 merge reads a flat object map. Any other shape is silently ignored there, so the builder has to emit exactly this.
        Assert.Equal("""{"Path":"/tmp/b"}""", req.CreateSteps.Single().ParametersJson);
    }

    [Fact]
    public void Build_CyclicDependencies_Throws()
    {
        var builder = JobWorkflowBuilder.New("etl").AddStep("A", LyoGuid.CreateV7(), dependsOn: ["B"]).AddStep("B", LyoGuid.CreateV7(), dependsOn: ["A"]);

        var ex = Assert.Throws<ValidationException>(builder.Build);
        Assert.Contains("circular", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithSchedule_AttachesWorkflowOwnedSchedule()
    {
        var req = JobWorkflowBuilder.New("etl").AddStep("A", LyoGuid.CreateV7()).WithSchedule(new() { Type = ScheduleType.Cron, CronExpression = "0 * * * *" }).Build();

        // The schedule's owner is filled in server-side from the workflow it is nested under, so the caller must not have to know its id in advance.
        var schedule = Assert.Single(req.CreateSchedules);
        Assert.Equal("0 * * * *", schedule.CronExpression);
    }

    private static string StepId(JobWorkflowReq req, string stepName) => req.CreateSteps.Single(s => s.StepName == stepName).Id.ToString();
}
