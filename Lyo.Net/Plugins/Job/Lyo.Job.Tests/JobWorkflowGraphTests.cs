using Lyo.Common.Core.Identifiers;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Tests;

/// <summary>
/// A circular <c>DependsOnStepIds</c> leaves every step pending and the run running forever, so cycle detection is what stands between a typo and a workflow that never
/// finishes. The diamond case matters just as much in the other direction: a shared ancestor is visited twice and must not be mistaken for a cycle.
/// </summary>
public class JobWorkflowGraphTests
{
    [Fact]
    public void FindDependencyCycle_SelfReference_IsACycle()
    {
        var a = LyoGuid.CreateV7();
        Assert.Contains(a, JobWorkflowGraph.FindDependencyCycle([Step(a, a.ToString())]));
    }

    [Fact]
    public void FindDependencyCycle_TwoStepLoop_ReturnsBothSteps()
    {
        var a = LyoGuid.CreateV7();
        var b = LyoGuid.CreateV7();
        Assert.Equal(2, JobWorkflowGraph.FindDependencyCycle([Step(a, b.ToString()), Step(b, a.ToString())]).Count);
    }

    [Fact]
    public void FindDependencyCycle_AcyclicDiamond_IsEmpty()
    {
        var a = LyoGuid.CreateV7();
        var b = LyoGuid.CreateV7();
        var c = LyoGuid.CreateV7();
        var d = LyoGuid.CreateV7();
        Assert.Empty(JobWorkflowGraph.FindDependencyCycle([Step(a, null), Step(b, a.ToString()), Step(c, a.ToString()), Step(d, $"{b},{c}")]));
    }

    [Fact]
    public void TryParseDependencies_BlankValue_SucceedsWithNoDependencies()
    {
        Assert.True(JobWorkflowGraph.TryParseDependencies(null, out var none));
        Assert.Empty(none);
    }

    [Fact]
    public void TryParseDependencies_NonGuidEntry_Fails()
        =>
            // Permanent configuration fault, not a transient one: callers fail the run rather than requeue it.
            Assert.False(JobWorkflowGraph.TryParseDependencies($"{LyoGuid.CreateV7()},not-a-guid", out var _));

    [Fact]
    public void TryParseDependencies_SpacedCsv_IsTrimmed()
    {
        var a = LyoGuid.CreateV7();
        var b = LyoGuid.CreateV7();
        Assert.True(JobWorkflowGraph.TryParseDependencies($" {a} , {b} ", out var parsed));
        Assert.Equal(2, parsed.Count);
        Assert.Contains(a, parsed);
        Assert.Contains(b, parsed);
    }

    private static JobWorkflowStepRes Step(Guid id, string? dependsOnStepIds)
        => new(id, Guid.Empty, Guid.Empty, id.ToString(), 0, dependsOnStepIds, JobWorkflowFailurePolicy.Stop, null, true);
}