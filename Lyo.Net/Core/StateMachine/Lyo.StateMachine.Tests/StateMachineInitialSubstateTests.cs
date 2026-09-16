using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineInitialSubstateTests
{
    [Fact]
    public void Fire_IntoParent_DrillsToInitialChildAndEntersBoth()
    {
        var builder = Job();
        builder.Configure(JobState.Running).InitialSubstate(JobState.Paused);
        var context = new JobContext();

        var result = builder.Build().Fire(context, At(JobState.Queued), JobTrigger.Start);

        Assert.Equal(JobState.Paused, result.To);
        Assert.Equal(["exit:Queued", "enter:Running", "enter:Paused"], context.Log);
    }

    [Fact]
    public void CreateInitialSnapshot_ParentInitial_DrillsDown()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).InitialSubstate(JobState.Paused);
        builder.Configure(JobState.Paused).SubstateOf(JobState.Running);

        Assert.Equal(JobState.Paused, builder.Build().CreateInitialSnapshot().State);
    }

    [Fact]
    public void Build_InitialSubstateNotChild_Throws()
    {
        var builder = Job();
        builder.Configure(JobState.Running).InitialSubstate(JobState.Done);
        Assert.Throws<StateMachineConfigurationException>(() => builder.Build());
    }
}
