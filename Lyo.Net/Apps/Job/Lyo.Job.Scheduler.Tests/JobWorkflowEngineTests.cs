using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;

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
        Assert.False(created.AllowTriggers);
    }

    [Fact]
    public void FindDependencyCycle_DetectsSelfReferenceTwoCycleAndAcyclicDiamond()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var selfReference = JobWorkflowEngine.FindDependencyCycle([BuildStep(a, workflowId, "A", 0, a.ToString())]);
        Assert.Contains(a, selfReference);

        var twoCycle = JobWorkflowEngine.FindDependencyCycle([BuildStep(a, workflowId, "A", 0, b.ToString()), BuildStep(b, workflowId, "B", 1, a.ToString())]);
        Assert.Equal(2, twoCycle.Count);

        // A -> {B, C} -> D is a diamond, not a cycle: visiting D twice cannot be mistaken for one.
        var diamond = JobWorkflowEngine.FindDependencyCycle([
            BuildStep(a, workflowId, "A", 0, null),
            BuildStep(b, workflowId, "B", 1, a.ToString()),
            BuildStep(c, workflowId, "C", 2, a.ToString()),
            BuildStep(d, workflowId, "D", 3, $"{b},{c}")
        ]);

        Assert.Empty(diamond);
    }

    private static JobWorkflowEngine CreateEngine(FakeWorkflowApiClient api)
        => new(new() { ApiBaseUrl = "http://localhost/api" }, api, new RecordingDelayedMqService());

    private static JobRunRes FinishedRun(Guid id, JobRunResult? result)
        => new() {
            Id = id,
            JobDefinitionId = Guid.NewGuid(),
            State = JobState.Finished,
            Result = result,
            CreatedTimestamp = DateTime.UtcNow.AddMinutes(-1),
            FinishedTimestamp = DateTime.UtcNow
        };

    private static JobWorkflowStepRes BuildStep(
        Guid id,
        Guid workflowId,
        string name,
        int order,
        string? dependsOnStepIds,
        JobWorkflowFailurePolicy policy = JobWorkflowFailurePolicy.Stop)
        => new(id, workflowId, Guid.NewGuid(), name, order, dependsOnStepIds, policy, null, true);

    /// <summary>A workflow run plus the pieces a test asserts against: the step definitions, their run steps, and the job run id whose completion drives the pass.</summary>
    private sealed record WorkflowGraph(JobWorkflowRunRes Run, JobWorkflowStepRes StepA, JobWorkflowStepRes? StepB, JobWorkflowRunStepRes? RunStepB, Guid FirstRunId)
    {
        public static WorkflowGraph SingleStep()
        {
            var workflowId = Guid.NewGuid();
            var runId = Guid.NewGuid();
            var stepA = BuildStep(Guid.NewGuid(), workflowId, "A", 0, null);
            var runStepA = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            return new(BuildRun(workflowId, [stepA], [runStepA]), stepA, null, null, runId);
        }

        /// <summary>A -&gt; B, where A has just completed and B is pending.</summary>
        public static WorkflowGraph Chain(JobWorkflowFailurePolicy policyOfA, string? dependsOnOverride = null)
        {
            var workflowId = Guid.NewGuid();
            var runId = Guid.NewGuid();
            var stepA = BuildStep(Guid.NewGuid(), workflowId, "A", 0, null, policyOfA);
            var stepB = BuildStep(Guid.NewGuid(), workflowId, "B", 1, dependsOnOverride ?? stepA.Id.ToString());
            var runStepA = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            var runStepB = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, stepB.Id, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepB], [runStepA, runStepB]), stepA, stepB, runStepB, runId);
        }

        /// <summary>A (completing) alongside S (already skipped) and B (pending, depends on S).</summary>
        public static WorkflowGraph SkippedDependency()
        {
            var workflowId = Guid.NewGuid();
            var runId = Guid.NewGuid();
            var stepA = BuildStep(Guid.NewGuid(), workflowId, "A", 0, null);
            var stepSkipped = BuildStep(Guid.NewGuid(), workflowId, "S", 1, null);
            var stepB = BuildStep(Guid.NewGuid(), workflowId, "B", 2, stepSkipped.Id.ToString());
            var runStepA = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, stepA.Id, runId, JobWorkflowStepState.Running);
            var runStepSkipped = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, stepSkipped.Id, null, JobWorkflowStepState.Skipped);
            var runStepB = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, stepB.Id, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepSkipped, stepB], [runStepA, runStepSkipped, runStepB]), stepA, stepB, runStepB, runId);
        }

        /// <summary>A -&gt; B -&gt; A. A has completed; without cycle detection both dependents stay pending and the run never finalizes.</summary>
        public static WorkflowGraph Cycle()
        {
            var workflowId = Guid.NewGuid();
            var runId = Guid.NewGuid();
            var idA = Guid.NewGuid();
            var idB = Guid.NewGuid();
            var stepA = BuildStep(idA, workflowId, "A", 0, idB.ToString());
            var stepB = BuildStep(idB, workflowId, "B", 1, idA.ToString());
            var runStepA = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, idA, runId, JobWorkflowStepState.Running);
            var runStepB = new JobWorkflowRunStepRes(Guid.NewGuid(), Guid.Empty, idB, null, JobWorkflowStepState.Pending);
            return new(BuildRun(workflowId, [stepA, stepB], [runStepA, runStepB]), stepA, stepB, runStepB, runId);
        }

        private static JobWorkflowRunRes BuildRun(Guid workflowId, IReadOnlyList<JobWorkflowStepRes> steps, IReadOnlyList<JobWorkflowRunStepRes> runSteps)
        {
            var runId = Guid.NewGuid();
            var workflow = new JobWorkflowRes(workflowId, "Workflow", null, true, steps);
            return new(runId, workflowId, JobWorkflowRunState.Running, DateTime.UtcNow.AddMinutes(-5), null, DateTime.UtcNow.AddMinutes(-5),
                runSteps.Select(s => s with { JobWorkflowRunId = runId }).ToList(), workflow);
        }
    }
}
