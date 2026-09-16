using static Lyo.StateMachine.Tests.TestMachines;

namespace Lyo.StateMachine.Tests;

[Trait("Category", "Fast")]
public class StateMachineHierarchyTests
{
    private readonly StateMachineDefinition<JobState, JobTrigger, JobContext> _definition = Job().Build();

    [Fact]
    public async Task FireAsync_SubstateInheritsParentPermit()
    {
        var result = await _definition.FireAsync(new(), At(JobState.Paused), JobTrigger.Complete, TestCt);
        Assert.Equal(JobState.Done, result.To);
    }

    [Fact]
    public async Task FireAsync_ChildPermitOverridesParent()
    {
        var result = await _definition.FireAsync(new(), At(JobState.Throttled), JobTrigger.Complete, TestCt);
        Assert.Equal(JobState.Queued, result.To);
    }

    [Fact]
    public async Task FireAsync_LeavingSubtree_ExitsChildThenParent()
    {
        var context = new JobContext();
        await _definition.FireAsync(context, At(JobState.Paused), JobTrigger.Complete, TestCt);
        Assert.Equal(["exit:Paused", "exit:Running", "enter:Done"], context.Log);
    }

    [Fact]
    public async Task FireAsync_EnteringSubtree_EntersParentThenChild()
    {
        var context = new JobContext();
        await _definition.FireAsync(context, At(JobState.Queued), JobTrigger.Pause, TestCt);
        Assert.Equal(["exit:Queued", "enter:Running", "enter:Paused"], context.Log);
    }

    [Fact]
    public async Task FireAsync_WithinParent_DoesNotExitParent()
    {
        var context = new JobContext();
        var result = await _definition.FireAsync(context, At(JobState.Paused), JobTrigger.Throttle, TestCt);
        Assert.Equal(JobState.Throttled, result.To);
        Assert.Equal(["exit:Paused", "enter:Throttled"], context.Log);
    }

    [Fact]
    public async Task FireAsync_ChildToParent_ExitsChildOnly()
    {
        var context = new JobContext();
        var result = await _definition.FireAsync(context, At(JobState.Paused), JobTrigger.Resume, TestCt);
        Assert.Equal(JobState.Running, result.To);
        Assert.Equal(["exit:Paused"], context.Log);
    }

    [Fact]
    public void GetSelfAndDescendants_ReturnsSubtree()
    {
        Assert.Equal([JobState.Running, JobState.Paused, JobState.Throttled], _definition.GetSelfAndDescendants(JobState.Running));
        Assert.Equal([JobState.Done], _definition.GetSelfAndDescendants(JobState.Done));
    }

    [Fact]
    public void IsInState_Descendant_ReturnsTrue()
    {
        Assert.True(_definition.IsInState(JobState.Paused, JobState.Running));
        Assert.True(_definition.IsInState(JobState.Paused, JobState.Paused));
        Assert.False(_definition.IsInState(JobState.Running, JobState.Paused));
        Assert.False(_definition.IsInState(JobState.Done, JobState.Running));
    }
}
