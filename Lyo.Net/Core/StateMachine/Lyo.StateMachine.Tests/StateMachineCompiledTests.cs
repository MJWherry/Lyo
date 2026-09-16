using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineCompiledTests
{
    private readonly StateMachineDefinition<JobState, JobTrigger, JobContext> _definition = Job().Build();

    [Theory]
    [InlineData(JobState.Paused, JobTrigger.Complete, new[] { "exit:Paused", "exit:Running", "enter:Done" })]
    [InlineData(JobState.Queued, JobTrigger.Pause, new[] { "exit:Queued", "enter:Running", "enter:Paused" })]
    [InlineData(JobState.Paused, JobTrigger.Throttle, new[] { "exit:Paused", "enter:Throttled" })]
    [InlineData(JobState.Paused, JobTrigger.Resume, new[] { "exit:Paused" })]
    [InlineData(JobState.Throttled, JobTrigger.Complete, new[] { "exit:Throttled", "exit:Running", "enter:Queued" })]
    public void Fire_InheritedRule_UsesPrecomputedPath(JobState from, JobTrigger trigger, string[] expected)
    {
        var context = new JobContext();
        _definition.Fire(context, At(from), trigger);
        Assert.Equal(expected, context.Log);
    }

    [Fact]
    public void FireAsync_NoAsyncHooks_CompletesSynchronously()
    {
        var task = _definition.FireAsync(new(), At(JobState.Queued), JobTrigger.Start, TestCt);
        Assert.True(task.IsCompletedSuccessfully);
    }
}
