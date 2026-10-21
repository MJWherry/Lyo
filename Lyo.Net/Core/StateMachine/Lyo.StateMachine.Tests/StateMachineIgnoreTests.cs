using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineIgnoreTests
{
    [Fact]
    public void Fire_Ignored_ReturnsSameSnapshotKindIgnored()
    {
        var builder = Job();
        builder.Configure(JobState.Done).Ignore(JobTrigger.Heartbeat);
        var context = new JobContext();
        var snapshot = At(JobState.Done, 3);
        var result = builder.Build().Fire(context, snapshot, JobTrigger.Heartbeat);
        Assert.True(result.Succeeded);
        Assert.False(result.Changed);
        Assert.Equal(TransitionKind.Ignored, result.Kind);
        Assert.Same(snapshot, result.Snapshot);
        Assert.Empty(context.Log);
    }

    [Fact]
    public void Fire_UnhandledThrowPolicy_ThrowsUnhandledTriggerException()
    {
        var definition = Job().OnUnhandledTrigger(UnhandledTriggerPolicy.Throw).Build();
        var ex = Assert.Throws<UnhandledTriggerException>(() => definition.Fire(new(), At(JobState.Done), JobTrigger.Start));
        Assert.Contains("Done", ex.Message);
        Assert.Contains("Start", ex.Message);
    }

    [Fact]
    public void Fire_UnhandledThrowPolicy_GuardFailureStillRejects()
    {
        var definition = Job().OnUnhandledTrigger(UnhandledTriggerPolicy.Throw).Build();
        var result = definition.Fire(new() { CanFail = false }, At(JobState.Queued), JobTrigger.Fail);
        Assert.Equal(TransitionRejection.GuardFailed, result.Rejection);
    }

    [Fact]
    public void Fire_UnhandledCallback_InvokedAndRejects()
    {
        var seen = new List<(JobState, JobTrigger)>();
        var definition = Job().OnUnhandledTrigger((_, state, trigger) => seen.Add((state, trigger))).Build();
        var result = definition.Fire(new(), At(JobState.Done), JobTrigger.Start);
        Assert.Equal(TransitionRejection.NoTransition, result.Rejection);
        Assert.Equal([(JobState.Done, JobTrigger.Start)], seen);
    }
}