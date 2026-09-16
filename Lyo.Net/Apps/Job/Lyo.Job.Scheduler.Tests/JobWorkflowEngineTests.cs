using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Formatter;
using Lyo.Job.Models.Builders;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Response;
using Lyo.Metrics;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Common.Core.Identifiers;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// Regression tests for the workflow engine fixes: a timed-out step cannot advance the DAG, a <see cref="JobWorkflowFailurePolicy.Continue" /> failure must release its
/// dependents instead of deadlocking the run, malformed or circular <c>DependsOnStepIds</c> must fail the run instead of hang or throw, and step runs must carry a deterministic
/// idempotency key.
/// </summary>
public class JobWorkflowEngineTests
{
    [Fact]
    public async Task TimedOutStep_IsMarkedFailed_AndFinalizesTheWorkflowAsFailed()
    {
        var graph = WorkflowGraph.SingleStep();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Timeout));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // A timeout used to fall through to Finished, advancing the DAG and finalizing the workflow as successful.
        Assert.Equal(JobWorkflowStepState.Failed, api.StepState(graph.StepA.Id));
        Assert.Equal(JobWorkflowRunState.Failed, api.WorkflowRun.State);
    }

    [Theory]
    [InlineData(JobRunResult.Success)]
    [InlineData(JobRunResult.SuccessWithWarnings)]
    [InlineData(JobRunResult.PartialSuccess)]
    [InlineData(JobRunResult.Skipped)]
    public async Task SuccessfulOutcomes_MarkTheStepFinished(JobRunResult result)
    {
        var graph = WorkflowGraph.SingleStep();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, result));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        Assert.Equal(JobWorkflowStepState.Finished, api.StepState(graph.StepA.Id));
        Assert.Equal(JobWorkflowRunState.Finished, api.WorkflowRun.State);
    }

    [Theory]
    [InlineData(JobRunResult.Failure)]
    [InlineData(JobRunResult.Cancelled)]
    [InlineData(JobRunResult.Unknown)]
    [InlineData(null)]
    public async Task FailureOutcomes_MarkTheStepFailed(JobRunResult? result)
    {
        var graph = WorkflowGraph.SingleStep();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, result));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        Assert.Equal(JobWorkflowStepState.Failed, api.StepState(graph.StepA.Id));
    }

    [Fact]
    public async Task FailedStepWithContinuePolicy_StartsItsDependent()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Continue);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Failure));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // Dependency satisfaction used to require Finished, so a Continue failure left B Pending forever and the run never finalized.
        Assert.Equal(JobWorkflowStepState.Failed, api.StepState(graph.StepA.Id));
        Assert.Equal(JobWorkflowStepState.Running, api.StepState(graph.StepB!.Id));
        Assert.Single(api.CreatedRunRequests);
        Assert.Equal(JobWorkflowRunState.Running, api.WorkflowRun.State);
    }

    [Fact]
    public async Task FailedStepWithStopPolicy_FailsTheRunAndSkipsPendingSteps()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Stop);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Failure));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        Assert.Equal(JobWorkflowRunState.Failed, api.WorkflowRun.State);
        Assert.Equal(JobWorkflowStepState.Skipped, api.StepState(graph.StepB!.Id));
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task SkippedDependency_DoesNotStartTheDependent()
    {
        var graph = WorkflowGraph.SkippedDependency();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success));

        // An unrelated sibling's completion drives the pass. The upstream work never ran, so Skipped cannot release the dependent.
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        Assert.Empty(api.CreatedRunRequests);
        Assert.Equal(JobWorkflowStepState.Pending, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task MalformedDependsOnStepIds_FailsTheRunInsteadOfThrowing()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Continue, "not-a-guid");
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success));

        // Guid.Parse used to throw here, bouncing the completion message through the requeue path five times before it was dropped.
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        Assert.Equal(JobWorkflowRunState.Failed, api.WorkflowRun.State);
        Assert.Equal(JobWorkflowStepState.Skipped, api.StepState(graph.StepB!.Id));
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task CircularDependencies_FailTheRunInsteadOfHanging()
    {
        var graph = WorkflowGraph.Cycle();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        Assert.Equal(JobWorkflowRunState.Failed, api.WorkflowRun.State);
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task StepRun_CarriesADeterministicIdempotencyKey()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Stop);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);
        var created = Assert.Single(api.CreatedRunRequests);

        // Two sibling completions racing (or a redelivered completion) must resolve to the same run instead of double-starting the step.
        Assert.Equal($"workflow:{graph.Run.Id:N}:{graph.RunStepB!.Id:N}", created.IdempotencyKey);
    }

    [Fact]
    public async Task StepRun_Parameters_MergeTheDefinitionDefaultWithTheStepsUpstreamTemplate()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Stop, parametersJsonForB: """{"Rows":"{Step.A.Result.RowCount}"}""");
        var definition = new JobDefinitionRes(
            graph.StepB!.JobDefinitionId, "Load", null, "Test", "cs", true,
            [new(LyoGuid.CreateV7(), graph.StepB.JobDefinitionId, "Rows", null, LyoTypeInfo.String.FullName, "0", null, true, false)], null, null);

        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("RowCount", "42"))).WithDefinition(definition);
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // The whole point of a workflow step: it reads what the step before it produced. Without the run-level bag and the batched upstream fetch this is the literal token.
        var created = Assert.Single(api.CreatedRunRequests);
        var parameter = Assert.Single(created.JobRunParameters);
        Assert.Equal("Rows", parameter.Key);
        Assert.Equal("42", parameter.Value);
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionMet_StartsTheConditionalStep()
    {
        var graph = WorkflowGraph.ConditionalChain();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        Assert.Equal(JobWorkflowStepState.Running, api.StepState(graph.StepB!.Id));
        Assert.Equal(JobWorkflowRunState.Running, api.WorkflowRun.State);
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionNotMet_MarksStepConditionNotMet()
    {
        var graph = WorkflowGraph.ConditionalChain();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "degraded")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // Distinct from Skipped: nothing went wrong and nothing stopped the workflow, the gate simply did not open.
        Assert.Equal(JobWorkflowStepState.ConditionNotMet, api.StepState(graph.StepB!.Id));
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionNotMet_DownstreamStepsAlsoConditionNotMet()
    {
        var graph = WorkflowGraph.ConditionalChain();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "degraded")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // C's input never materialized. Leaving it Pending would deadlock the run; running it would feed it nothing.
        Assert.Equal(JobWorkflowStepState.ConditionNotMet, api.StepState(graph.StepC!.Id));
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_AllRemainingStepsConditionNotMet_FinalizesRunFinished()
    {
        var graph = WorkflowGraph.ConditionalChain();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "degraded")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // A skipped tail is a successful workflow, not a hung one: the completion that marked the last steps is the last event this run will ever get.
        Assert.Equal(JobWorkflowRunState.Finished, api.WorkflowRun.State);
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_AndGroupWithOneChildUnmet_MarksStepConditionNotMet()
    {
        var condition = JobWorkflowStepCondition.All(
            JobWorkflowStepCondition.When("A", "Status", ComparisonOperatorEnum.Equals, "ok"),
            JobWorkflowStepCondition.When("A", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"));

        var graph = WorkflowGraph.ConditionalChain(condition);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok"), ("RowCount", "0")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        Assert.Equal(JobWorkflowStepState.ConditionNotMet, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_AndGroupFullyMet_StartsTheConditionalStep()
    {
        var condition = JobWorkflowStepCondition.All(
            JobWorkflowStepCondition.When("A", "Status", ComparisonOperatorEnum.Equals, "ok"),
            JobWorkflowStepCondition.When("A", "RowCount", ComparisonOperatorEnum.GreaterThan, "0"));

        var graph = WorkflowGraph.ConditionalChain(condition);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok"), ("RowCount", "42")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        Assert.Equal(JobWorkflowStepState.Running, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionOnRunInput_GatesTheStep()
    {
        var condition = JobWorkflowStepCondition.WhenPath("Input.Env", ComparisonOperatorEnum.Equals, "prod");
        var graph = WorkflowGraph.ConditionalChain(condition, """{"Env":"stage"}""");
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // The branch is decided by what the run was started with, not by anything an upstream step produced.
        Assert.Equal(JobWorkflowStepState.ConditionNotMet, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionOnRunInputMet_StartsTheStep()
    {
        var condition = JobWorkflowStepCondition.WhenPath("Input.Env", ComparisonOperatorEnum.Equals, "prod");
        var graph = WorkflowGraph.ConditionalChain(condition, """{"Env":"prod"}""");
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        Assert.Equal(JobWorkflowStepState.Running, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionOnWorkflowField_GatesTheStep()
    {
        var condition = JobWorkflowStepCondition.WhenPath("Workflow.Name", ComparisonOperatorEnum.Equals, "Some other workflow");
        var graph = WorkflowGraph.ConditionalChain(condition);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // Workflow is seeded as an object, so this only gates if the run scope reaches into its properties.
        Assert.Equal(JobWorkflowStepState.ConditionNotMet, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task ProcessRunCompletionAsync_ConditionOnStepDefinitionField_GatesTheStep()
    {
        var condition = JobWorkflowStepCondition.WhenPath("CurrentStep.StepName", ComparisonOperatorEnum.Equals, "NotB");
        var graph = WorkflowGraph.ConditionalChain(condition);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "ok")));
        await CreateEngine(api).ProcessRunCompletionAsync(graph.FirstRunId);

        // The step scope has to exist before the condition pass, not only for the steps that were already cleared to start.
        Assert.Equal(JobWorkflowStepState.ConditionNotMet, api.StepState(graph.StepB!.Id));
    }

    [Fact]
    public async Task ProcessWorkflowRunCreatedAsync_PendingRun_StartsRootStepsAndMovesRunToRunning()
    {
        var graph = WorkflowGraph.PendingChain();
        var api = new FakeWorkflowApiClient(graph.Run);
        await CreateEngine(api).ProcessWorkflowRunCreatedAsync(graph.Run.Id);

        // Only the root starts. B depends on A, so it has to stay Pending until A's completion drives the DAG forward.
        Assert.Single(api.CreatedRunRequests);
        Assert.Equal(JobWorkflowStepState.Running, api.StepState(graph.StepA.Id));
        Assert.Equal(JobWorkflowStepState.Pending, api.StepState(graph.StepB!.Id));
        Assert.Equal(JobWorkflowRunState.Running, api.WorkflowRun.State);
    }

    [Fact]
    public async Task ProcessWorkflowRunCreatedAsync_AlreadyRunningRun_StartsNothing()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Stop);
        var api = new FakeWorkflowApiClient(graph.Run);
        await CreateEngine(api).ProcessWorkflowRunCreatedAsync(graph.Run.Id);

        // A redelivered created-event must not re-dispatch the roots of a run the engine already picked up.
        Assert.Empty(api.CreatedRunRequests);
    }

    [Fact]
    public async Task StopPolicyFailure_PublishesFailureAlert()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Stop);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Failure));
        var publisher = new FakeEventPublisher();
        await CreateEngine(api, eventPublisher: publisher).ProcessRunCompletionAsync(graph.FirstRunId);

        // JobDefinition.AlertOnFailure is gone, so the workflow engine is what keeps JobAlertType.Failure reachable.
        var alert = Assert.Single(publisher.Alerts);
        Assert.Equal(JobAlertType.Failure, alert.AlertType);
        Assert.Equal(graph.FirstRunId, alert.RunId);
        Assert.Contains("Workflow", alert.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunFinished_IncrementsRunsFinishedCounter()
    {
        var graph = WorkflowGraph.SingleStep();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success));
        var metrics = new RecordingMetrics();
        await CreateEngine(api, metrics).ProcessRunCompletionAsync(graph.FirstRunId);

        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.RunsFinished));
        Assert.Equal(0, metrics.Count(Constants.Metrics.Workflow.RunsFailed));
    }

    [Fact]
    public async Task RunFailed_IncrementsRunsFailedAndStepsFailedCounters()
    {
        var graph = WorkflowGraph.Chain(JobWorkflowFailurePolicy.Stop);
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Failure));
        var metrics = new RecordingMetrics();
        await CreateEngine(api, metrics).ProcessRunCompletionAsync(graph.FirstRunId);

        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.StepsFailed));
        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.RunsFailed));
        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.StepsSkipped));
    }

    [Fact]
    public async Task ConditionNotMetStep_IncrementsStepsSkippedCounter()
    {
        var graph = WorkflowGraph.ConditionalChain();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success, ("Status", "degraded")));
        var metrics = new RecordingMetrics();
        await CreateEngine(api, metrics).ProcessRunCompletionAsync(graph.FirstRunId);

        // B is gated off and C cascades, so a gated tail is two skips, not a silent nothing.
        Assert.Equal(2, metrics.Count(Constants.Metrics.Workflow.StepsSkipped));
    }

    [Fact]
    public async Task CircularDependencies_IncrementGraphInvalidCounter()
    {
        var graph = WorkflowGraph.Cycle();
        var api = new FakeWorkflowApiClient(graph.Run).WithJobRun(FinishedRun(graph.FirstRunId, JobRunResult.Success));
        var metrics = new RecordingMetrics();
        await CreateEngine(api, metrics).ProcessRunCompletionAsync(graph.FirstRunId);

        // A hung run looks identical to a slow one from the outside. This counter is what makes a malformed graph visible.
        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.GraphInvalid));
        Assert.Equal(JobWorkflowRunState.Failed, api.WorkflowRun.State);
    }

    [Fact]
    public async Task StartedStep_IncrementsStepsStartedCounter()
    {
        var graph = WorkflowGraph.PendingChain();
        var api = new FakeWorkflowApiClient(graph.Run);
        var metrics = new RecordingMetrics();
        await CreateEngine(api, metrics).ProcessWorkflowRunCreatedAsync(graph.Run.Id);

        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.StepsStarted));
        Assert.Equal(1, metrics.Count(Constants.Metrics.Workflow.RunsStarted));
    }

    private static JobWorkflowEngine CreateEngine(FakeWorkflowApiClient api, IMetrics? metrics = null, IJobEventPublisher? eventPublisher = null)
        => new(
            new() { ApiBaseUrl = "http://localhost/api" }, api, new RecordingDelayedMqService(), new FormatterService(() => DateTimeOffset.UtcNow), null, eventPublisher,
            metrics);

    private static JobRunRes FinishedRun(Guid id, JobRunResult? result, params (string Key, string Value)[] results)
        => new() {
            Id = id,
            JobDefinitionId = LyoGuid.CreateV7(),
            State = JobState.Finished,
            Result = result,
            CreatedTimestamp = DateTime.UtcNow.AddMinutes(-1),
            FinishedTimestamp = DateTime.UtcNow,
            JobRunResults = [.. results.Select(r => new JobRunResultRes(LyoGuid.CreateV7(), id, r.Key, LyoTypeInfo.String.FullName, r.Value))]
        };

    private static JobWorkflowStepRes BuildStep(
        Guid id,
        Guid workflowId,
        string name,
        int order,
        string? dependsOnStepIds,
        JobWorkflowFailurePolicy policy = JobWorkflowFailurePolicy.Stop,
        string? parametersJson = null)
        => new(id, workflowId, LyoGuid.CreateV7(), name, order, dependsOnStepIds, policy, parametersJson, true);

    /// <summary>A workflow run plus the pieces a test asserts against: the step definitions, their run steps, and the job run id whose completion drives the pass.</summary>
    private sealed record WorkflowGraph(
        JobWorkflowRunRes Run,
        JobWorkflowStepRes StepA,
        JobWorkflowStepRes? StepB,
        JobWorkflowRunStepRes? RunStepB,
        Guid FirstRunId,
        JobWorkflowStepRes? StepC = null)
    {
        public static WorkflowGraph SingleStep()
        {
            var workflowId = LyoGuid.CreateV7();
            var runId = LyoGuid.CreateV7();
            var stepA = BuildStep(LyoGuid.CreateV7(), workflowId, "A", 0, null);
            var runStepA = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            return new(BuildRun(workflowId, [stepA], [runStepA]), stepA, null, null, runId);
        }

        /// <summary>A -&gt; B, where A has just completed and B is pending.</summary>
        public static WorkflowGraph Chain(JobWorkflowFailurePolicy policyOfA, string? dependsOnOverride = null, string? parametersJsonForB = null)
        {
            var workflowId = LyoGuid.CreateV7();
            var runId = LyoGuid.CreateV7();
            var stepA = BuildStep(LyoGuid.CreateV7(), workflowId, "A", 0, null, policyOfA);
            var stepB = BuildStep(LyoGuid.CreateV7(), workflowId, "B", 1, dependsOnOverride ?? stepA.Id.ToString(), parametersJson: parametersJsonForB);
            var runStepA = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            var runStepB = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepB.Id, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepB], [runStepA, runStepB]), stepA, stepB, runStepB, runId);
        }

        /// <summary>A (completing) alongside S (already skipped) and B (pending, depends on S).</summary>
        public static WorkflowGraph SkippedDependency()
        {
            var workflowId = LyoGuid.CreateV7();
            var runId = LyoGuid.CreateV7();
            var stepA = BuildStep(LyoGuid.CreateV7(), workflowId, "A", 0, null);
            var stepSkipped = BuildStep(LyoGuid.CreateV7(), workflowId, "S", 1, null);
            var stepB = BuildStep(LyoGuid.CreateV7(), workflowId, "B", 2, stepSkipped.Id.ToString());
            var runStepA = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            var runStepSkipped = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepSkipped.Id, null, JobWorkflowStepState.Skipped);
            var runStepB = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepB.Id, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepSkipped, stepB], [runStepA, runStepSkipped, runStepB]), stepA, stepB, runStepB, runId);
        }

        /// <summary>A -&gt; B -&gt; C where B is gated by <paramref name="condition" />, defaulting to "A reported Status = ok". A has just completed; B and C are pending.</summary>
        public static WorkflowGraph ConditionalChain(WhereClause? condition = null, string? inputsJson = null)
        {
            var workflowId = LyoGuid.CreateV7();
            var runId = LyoGuid.CreateV7();
            var stepA = BuildStep(LyoGuid.CreateV7(), workflowId, "A", 0, null);
            var clause = condition ?? JobWorkflowStepCondition.When("A", "Status", ComparisonOperatorEnum.Equals, "ok");
            var stepB = BuildStep(LyoGuid.CreateV7(), workflowId, "B", 1, stepA.Id.ToString()) with { ConditionJson = JsonSerializer.Serialize(clause) };

            var stepC = BuildStep(LyoGuid.CreateV7(), workflowId, "C", 2, stepB.Id.ToString());
            var runStepA = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            var runStepB = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepB.Id, null, JobWorkflowStepState.Pending);
            var runStepC = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepC.Id, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepB, stepC], [runStepA, runStepB, runStepC], inputsJson: inputsJson), stepA, stepB, runStepB, runId, stepC);
        }

        /// <summary>A -&gt; B where neither step has started and the run is still Pending, as a freshly materialized run arrives at the engine.</summary>
        public static WorkflowGraph PendingChain()
        {
            var workflowId = LyoGuid.CreateV7();
            var stepA = BuildStep(LyoGuid.CreateV7(), workflowId, "A", 0, null);
            var stepB = BuildStep(LyoGuid.CreateV7(), workflowId, "B", 1, stepA.Id.ToString());
            var runStepA = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepA.Id, null, JobWorkflowStepState.Pending);
            var runStepB = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, stepB.Id, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepB], [runStepA, runStepB], JobWorkflowRunState.Pending), stepA, stepB, runStepB, Guid.Empty);
        }

        /// <summary>A -&gt; B -&gt; A. A has completed; without cycle detection both dependents stay pending and the run never finalizes.</summary>
        public static WorkflowGraph Cycle()
        {
            var workflowId = LyoGuid.CreateV7();
            var runId = LyoGuid.CreateV7();
            var idA = LyoGuid.CreateV7();
            var idB = LyoGuid.CreateV7();
            var stepA = BuildStep(idA, workflowId, "A", 0, idB.ToString());
            var stepB = BuildStep(idB, workflowId, "B", 1, idA.ToString());
            var runStepA = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, idA, runId, JobWorkflowStepState.Running);
            var runStepB = new JobWorkflowRunStepRes(LyoGuid.CreateV7(), Guid.Empty, idB, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepB], [runStepA, runStepB]), stepA, stepB, runStepB, runId);
        }

        private static JobWorkflowRunRes BuildRun(
            Guid workflowId,
            IReadOnlyList<JobWorkflowStepRes> steps,
            IReadOnlyList<JobWorkflowRunStepRes> runSteps,
            JobWorkflowRunState state = JobWorkflowRunState.Running,
            string? inputsJson = null)
        {
            var runId = LyoGuid.CreateV7();
            var workflow = new JobWorkflowRes(workflowId, "Workflow", null, true, steps);
            var started = state == JobWorkflowRunState.Pending ? null : (DateTime?)DateTime.UtcNow.AddMinutes(-5);
            return new(runId, workflowId, state, started, null, DateTime.UtcNow.AddMinutes(-5),
                runSteps.Select(s => s with { JobWorkflowRunId = runId }).ToList(), workflow, InputsJson: inputsJson);
        }
    }
}
