using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineHardeningTests
{
    [Fact]
    public void Fire_HookThrows_WrapsWithStateAndTrigger()
    {
        var boom = new InvalidOperationException("boom");
        var builder = Job();
        builder.Configure(JobState.Done).OnEntry(_ => throw boom);

        var ex = Assert.Throws<StateMachineHookException>(() => builder.Build().Fire(new(), At(JobState.Running), JobTrigger.Complete));

        Assert.Same(boom, ex.InnerException);
        Assert.Contains("'job'", ex.Message);
        Assert.Contains("entry", ex.Message);
        Assert.Contains("Done", ex.Message);
        Assert.Contains("Complete", ex.Message);
    }

    [Fact]
    public void Fire_GuardThrows_Wrapped()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).PermitIf(JobTrigger.Complete, JobState.Done, _ => throw new FormatException("bad"));

        var ex = Assert.Throws<StateMachineHookException>(() => builder.Build().Fire(new(), At(JobState.Running), JobTrigger.Complete));

        Assert.IsType<FormatException>(ex.InnerException);
        Assert.Contains("guard", ex.Message);
    }

    [Fact]
    public void Fire_NonUtcSnapshot_Throws()
    {
        var snapshot = new StateSnapshot<JobState>(JobState.Queued, 0, new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        Assert.Throws<ArgumentException>(() => Job().Build().Fire(new(), snapshot, JobTrigger.Start));
    }

    [Fact]
    public void Build_NonUtcClock_ThrowsOnFire()
    {
        var definition = Job().UseClock(() => new(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)).Build();
        Assert.Throws<StateMachineConfigurationException>(() => definition.Fire(new(), At(JobState.Queued), JobTrigger.Start));
    }

    [Fact]
    public async Task FireAsync_CancelledBetweenHooks_Throws()
    {
        using var cts = new CancellationTokenSource();
        var builder = Job();
        builder.Configure(JobState.Running).OnExit(_ => cts.Cancel());
        var context = new JobContext();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => builder.Build().FireAsync(context, At(JobState.Running), JobTrigger.Complete, cts.Token));

        Assert.DoesNotContain("enter:Done", context.Log);
    }

    [Fact]
    public async Task FireAsync_GuardReentrancy_HookFiringSameContextThrows()
    {
        StateMachineDefinition<JobState, JobTrigger, JobContext>? definition = null;
        var builder = Job().GuardReentrancy();
        builder.Configure(JobState.Done).OnEntryAsync((t, ct) => definition!.FireAsync(t.Context, At(JobState.Queued), JobTrigger.Start, ct));
        definition = builder.Build();

        await Assert.ThrowsAsync<StateMachineReentrancyException>(() => definition.FireAsync(new(), At(JobState.Running), JobTrigger.Complete, TestCt));
    }

    [Fact]
    public async Task FireAsync_GuardReentrancy_OtherContextAllowed()
    {
        StateMachineDefinition<JobState, JobTrigger, JobContext>? definition = null;
        TransitionResult<JobState>? inner = null;
        var builder = Job().GuardReentrancy();
        builder.Configure(JobState.Done).OnEntryAsync(async (_, ct) => inner = await definition!.FireAsync(new(), At(JobState.Queued), JobTrigger.Start, ct));
        definition = builder.Build();

        await definition.FireAsync(new(), At(JobState.Running), JobTrigger.Complete, TestCt);

        Assert.True(inner!.Succeeded);
    }
}
