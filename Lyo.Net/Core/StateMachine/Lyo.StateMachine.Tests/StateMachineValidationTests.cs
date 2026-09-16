namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineValidationTests
{
    private static StateMachineBuilder<JobState, JobTrigger, JobContext> Machine()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Queued);
        builder.Configure(JobState.Queued).Permit(JobTrigger.Start, JobState.Running);
        builder.Configure(JobState.Running).Permit(JobTrigger.Complete, JobState.Done);
        builder.Configure(JobState.Done).AsFinal();
        return builder;
    }

    [Fact]
    public void Analyze_ReportsUnreachableAndDeadEndStates()
    {
        var builder = Machine();
        builder.Configure(JobState.Failed);
        builder.Configure(JobState.Paused).Permit(JobTrigger.Resume, JobState.Running);

        var analysis = builder.Build().Analyze();

        Assert.False(analysis.IsClean);
        Assert.Equal([JobState.Failed, JobState.Paused], analysis.UnreachableStates);
        Assert.Equal([JobState.Failed], analysis.DeadEndStates);
    }

    [Fact]
    public void Build_Strict_UnreachableThrows()
    {
        var builder = Machine().Strict();
        builder.Configure(JobState.Paused).Permit(JobTrigger.Resume, JobState.Running);

        var ex = Assert.Throws<StateMachineConfigurationException>(() => builder.Build());

        Assert.Contains("Paused", ex.Message);
    }

    [Fact]
    public void Build_Strict_FinalDeadEndAllowed() => Assert.True(Machine().Strict().Build().Analyze().IsClean);
}
