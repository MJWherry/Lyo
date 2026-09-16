using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineDynamicTests
{
    private static StateMachineBuilder<JobState, JobTrigger, JobContext> Machine()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running)
            .PermitDynamic(JobTrigger.Complete, c => c.CanFail ? JobState.Failed : JobState.Done, JobState.Done, JobState.Failed)
            .PermitDynamic(Retry, (c, p) => p.Attempt >= c.MaxAttempts ? JobState.Failed : JobState.Queued, JobState.Queued, JobState.Failed)
            .OnExit(t => t.Context.Log.Add("exit:Running"));

        builder.Configure(JobState.Failed).OnEntry(t => t.Context.Log.Add("enter:Failed"));
        return builder;
    }

    [Theory]
    [InlineData(false, JobState.Done)]
    [InlineData(true, JobState.Failed)]
    public void Fire_Dynamic_SelectsDestinationFromContext(bool canFail, JobState expected)
    {
        var result = Machine().Build().Fire(new() { CanFail = canFail }, At(JobState.Running), JobTrigger.Complete);
        Assert.Equal(expected, result.To);
        Assert.Equal(TransitionKind.Transition, result.Kind);
    }

    [Fact]
    public void Fire_DynamicPayload_SelectsFromPayload()
    {
        var definition = Machine().Build();
        var context = new JobContext { MaxAttempts = 3 };

        Assert.Equal(JobState.Queued, definition.Fire(new(), At(JobState.Running), Retry, new(1)).To);
        Assert.Equal(JobState.Failed, definition.Fire(context, At(JobState.Running), Retry, new(3)).To);
        Assert.Equal(["exit:Running", "enter:Failed"], context.Log);
    }

    [Fact]
    public void Fire_DynamicUndeclaredDestination_Throws()
    {
        var builder = new StateMachineBuilder<JobState, JobTrigger, JobContext>("job", JobState.Running);
        builder.Configure(JobState.Running).PermitDynamic(JobTrigger.Complete, _ => JobState.Paused, JobState.Done);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build().Fire(new(), At(JobState.Running), JobTrigger.Complete));

        Assert.Contains("Paused", ex.Message);
        Assert.Contains("Complete", ex.Message);
    }

    [Fact]
    public void ToMermaid_Dynamic_ListsEveryPossibleDestination()
    {
        var mermaid = Machine().Build().ToMermaid();
        Assert.Contains("    Running --> Done : Complete (dynamic)\n", mermaid);
        Assert.Contains("    Running --> Failed : Complete (dynamic)\n", mermaid);
        Assert.Contains("    Running --> Queued : Retry (dynamic)\n", mermaid);
    }
}
