using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineDefinitionTests
{
    [Fact]
    public async Task FireAsync_PermittedTrigger_ReturnsNextStateAndIncrementsVersion()
    {
        var clock = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
        var definition = Job().UseClock(() => clock).Build();
        var result = await definition.FireAsync(new(), At(JobState.Queued), JobTrigger.Start, TestCt);
        Assert.True(result.Succeeded);
        Assert.Equal(TransitionRejection.None, result.Rejection);
        Assert.Equal(JobState.Queued, result.From);
        Assert.Equal(JobState.Running, result.To);
        Assert.Equal(1, result.Snapshot.Version);
        Assert.Equal(clock, result.Snapshot.EnteredAtUtc);
    }

    [Fact]
    public async Task FireAsync_UnknownTrigger_ReturnsNoTransitionRejection()
    {
        var definition = Job().Build();
        var context = new JobContext();
        var snapshot = At(JobState.Queued, 4);
        var result = await definition.FireAsync(context, snapshot, JobTrigger.Complete, TestCt);
        Assert.False(result.Succeeded);
        Assert.Equal(TransitionRejection.NoTransition, result.Rejection);
        Assert.Same(snapshot, result.Snapshot);
        Assert.Empty(context.Log);
    }

    [Fact]
    public async Task FireAsync_GuardFalse_ReturnsGuardFailed()
    {
        var definition = Job().Build();
        var context = new JobContext { MaxAttempts = 2 };
        var result = await definition.FireAsync(context, At(JobState.Running), Retry, new(2), TestCt);
        Assert.Equal(TransitionRejection.GuardFailed, result.Rejection);
        Assert.Equal(JobState.Running, result.To);
        Assert.Empty(context.Log);
    }

    [Fact]
    public async Task FireAsync_TypedPayload_ReachesGuardAndActions()
    {
        var seenByGuard = 0;
        var seenByEntry = 0;
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).PermitIf(Retry, JobState.Queued, (_, p) => (seenByGuard = p.Attempt) < 5);
        builder.Configure(JobState.Queued).OnEntryFrom(Retry, (_, p) => seenByEntry = p.Attempt);
        var result = await builder.Build().FireAsync(new(), At(JobState.Running), Retry, new(3), TestCt);
        Assert.True(result.Succeeded);
        Assert.Equal(3, seenByGuard);
        Assert.Equal(3, seenByEntry);
    }

    [Fact]
    public async Task FireAsync_PayloadTriggerWithoutPayload_Throws()
    {
        var definition = Job().Build();
        await Assert.ThrowsAsync<ArgumentException>(() => definition.FireAsync(new(), At(JobState.Running), JobTrigger.Retry, TestCt));
    }

    [Fact]
    public async Task FireAsync_Hooks_RunExitTransitionEntryInOrder()
    {
        var log = new List<string>();
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).Permit(JobTrigger.Complete, JobState.Done).OnExit(_ => log.Add("exit"));
        builder.Configure(JobState.Done)
            .OnEntryAsync((_, _) => {
                log.Add("entry");
                return Task.CompletedTask;
            });

        builder.OnTransition(t => log.Add($"transition:{t.Source}->{t.Destination}:{t.Trigger}"));
        builder.AddObserver(new RecordingObserver(log));
        await builder.Build().FireAsync(new(), At(JobState.Running), JobTrigger.Complete, TestCt);
        Assert.Equal(["exit", "transition:Running->Done:Complete", "entry", "observer:Running->Done@1"], log);
    }

    [Fact]
    public async Task FireAsync_Reentry_RunsExitAndEntry()
    {
        var definition = Job().Build();
        var context = new JobContext();
        var result = await definition.FireAsync(context, At(JobState.Running, 7), JobTrigger.Heartbeat, TestCt);
        Assert.True(result.Succeeded);
        Assert.Equal(JobState.Running, result.To);
        Assert.Equal(8, result.Snapshot.Version);
        Assert.Equal(["exit:Running", "enter:Running"], context.Log);
    }

    [Fact]
    public void Fire_AsyncHookRegistered_Throws()
    {
        var builder = Job();
        builder.Configure(JobState.Done).OnEntryAsync((_, _) => Task.CompletedTask);
        var definition = builder.Build();
        Assert.True(definition.HasAsyncHooks);
        Assert.Throws<StateMachineConfigurationException>(() => definition.Fire(new(), At(JobState.Running), JobTrigger.Complete));
    }

    [Fact]
    public async Task Fire_SyncOnly_Transitions()
    {
        var definition = Job().UseClock(() => DateTime.UnixEpoch).Build();
        var syncContext = new JobContext();
        var asyncContext = new JobContext();
        var sync = definition.Fire(syncContext, At(JobState.Running), Retry, new(1));
        var async = await definition.FireAsync(asyncContext, At(JobState.Running), Retry, new(1), TestCt);
        Assert.Equal(async, sync);
        Assert.Equal(JobState.Queued, sync.To);
        Assert.Equal(asyncContext.Log, syncContext.Log);
    }

    [Fact]
    public void Build_DuplicateUnguardedPermit_Throws()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running).Permit(JobTrigger.Start, JobState.Done);
        Assert.Throws<StateMachineConfigurationException>(() => builder.Build());
    }

    [Fact]
    public void Build_SubstateCycle_Throws()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Paused).SubstateOf(JobState.Running);
        builder.Configure(JobState.Running).SubstateOf(JobState.Paused);
        Assert.Throws<StateMachineConfigurationException>(() => builder.Build());
    }

    [Theory]
    [InlineData(false, new[] { JobTrigger.Start, JobTrigger.Pause })]
    [InlineData(true, new[] { JobTrigger.Start, JobTrigger.Pause, JobTrigger.Fail })]
    public void GetPermittedTriggers_WithGuards_ReturnsOnlyPassing(bool canFail, JobTrigger[] expected)
    {
        var definition = Job().Build();
        Assert.Equal(expected, definition.GetPermittedTriggers(new() { CanFail = canFail }, JobState.Queued));
    }
}