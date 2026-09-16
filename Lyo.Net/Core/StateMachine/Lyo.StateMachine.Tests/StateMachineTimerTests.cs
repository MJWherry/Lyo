using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineTimerTests
{
    private static readonly DateTime T0 = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);
    private DateTime _now = T0;

    private StateMachineBuilder<JobState, JobTrigger, JobContext> Machine()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued).UseClock(() => _now);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
        builder.Configure(JobState.Running)
            .Permit(JobTrigger.Complete, JobState.Done)
            .Permit(JobTrigger.Fail, JobState.Failed)
            .PermitReentry(JobTrigger.Heartbeat)
            .After(TimeSpan.FromSeconds(30), JobTrigger.Fail)
            .After(TimeSpan.FromSeconds(10), JobTrigger.Complete);

        return builder;
    }

    [Fact]
    public void Fire_IntoTimedState_SetsDueAtFromEarliestTimer()
    {
        var result = Machine().Build().Fire(new(), At(JobState.Queued), JobTrigger.Start);
        Assert.Equal(T0.AddSeconds(10), result.Snapshot.DueAtUtc);
    }

    [Fact]
    public void Fire_OutOfTimedState_ClearsDueAt()
    {
        var definition = Machine().Build();
        var running = definition.Fire(new(), At(JobState.Queued), JobTrigger.Start).Snapshot;
        Assert.Null(definition.Fire(new(), running, JobTrigger.Complete).Snapshot.DueAtUtc);
    }

    [Fact]
    public void GetDueTrigger_BeforeDue_ReturnsNull()
    {
        var definition = Machine().Build();
        Assert.Null(definition.GetDueTrigger(new(JobState.Running, 1, T0), T0.AddSeconds(9)));
    }

    [Fact]
    public void GetDueTrigger_AfterDue_ReturnsTrigger()
    {
        var definition = Machine().Build();
        var due = definition.GetDueTrigger(new(JobState.Running, 1, T0), T0.AddSeconds(10));
        Assert.Equal(new StateTimer<JobTrigger>(JobTrigger.Complete, T0.AddSeconds(10)), due);
    }

    [Fact]
    public void Build_TimerTriggerNotPermitted_Throws()
    {
        var builder = Machine();
        builder.Configure(JobState.Running).After(TimeSpan.FromSeconds(1), JobTrigger.Resume);
        Assert.Throws<StateMachineConfigurationException>(() => builder.Build());
    }

    [Fact]
    public void Build_TimerTriggerWithPayload_Throws()
    {
        var builder = Machine();
        builder.Configure(JobState.Running).Permit(Retry, JobState.Queued).After(TimeSpan.FromSeconds(1), JobTrigger.Retry);
        Assert.Throws<StateMachineConfigurationException>(() => builder.Build());
    }

    [Fact]
    public void Fire_Reentry_RearmsTimer()
    {
        var definition = Machine().Build();
        _now = T0.AddSeconds(5);

        var result = definition.Fire(new(), new(JobState.Running, 1, T0, T0.AddSeconds(10)), JobTrigger.Heartbeat);

        Assert.Equal(T0.AddSeconds(5), result.Snapshot.EnteredAtUtc);
        Assert.Equal(T0.AddSeconds(15), result.Snapshot.DueAtUtc);
    }

    [Fact]
    public void CreateInitialSnapshot_TimedInitialState_SetsDueAt()
    {
        var builder = Machine();
        builder.Configure(JobState.Queued).After(TimeSpan.FromMinutes(1), JobTrigger.Start);
        Assert.Equal(T0.AddMinutes(1), builder.Build().CreateInitialSnapshot().DueAtUtc);
    }
}
