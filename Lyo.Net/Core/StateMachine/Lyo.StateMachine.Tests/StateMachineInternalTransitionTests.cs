using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineInternalTransitionTests
{
    [Fact]
    public void Fire_Internal_RunsActionOnlyVersionUnchanged()
    {
        var builder = Job();
        builder.Configure(JobState.Done).InternalTransition(JobTrigger.Heartbeat, t => t.Context.Log.Add($"internal:{t.Kind}"));
        var context = new JobContext();
        var snapshot = At(JobState.Done, 5);

        var result = builder.Build().Fire(context, snapshot, JobTrigger.Heartbeat);

        Assert.Equal(TransitionKind.Internal, result.Kind);
        Assert.Same(snapshot, result.Snapshot);
        Assert.Equal(["internal:Internal"], context.Log);
    }

    [Fact]
    public async Task Fire_InternalWithPayload_ActionGetsTypedPayload()
    {
        var seen = 0;
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).InternalTransitionAsync(Retry, (_, p, _) => {
            seen = p.Attempt;
            return Task.CompletedTask;
        });

        var result = await builder.Build().FireAsync(new(), At(JobState.Running), Retry, new(4), TestCt);

        Assert.Equal(TransitionKind.Internal, result.Kind);
        Assert.Equal(4, seen);
    }

    [Fact]
    public void Fire_InternalGuardFalse_GuardFailed()
    {
        var builder = Job();
        builder.Configure(JobState.Done).InternalTransitionIf(JobTrigger.Heartbeat, c => c.CanFail, t => t.Context.Log.Add("internal"));
        var context = new JobContext { CanFail = false };

        var result = builder.Build().Fire(context, At(JobState.Done), JobTrigger.Heartbeat);

        Assert.Equal(TransitionRejection.GuardFailed, result.Rejection);
        Assert.Empty(context.Log);
    }
}
