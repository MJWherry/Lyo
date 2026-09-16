using Lyo.Job.Models.Builders;
using Lyo.Job.Web.Components;
using Lyo.Query.Models.Enums;

namespace Lyo.Job.Scheduler.Tests;

public class JobWorkflowDiagramTests
{
    [Fact]
    public void Create_Chain_PlacesEachStepAndConnectsThem()
    {
        var picture = JobWorkflowDiagram.Create("Checkout", Steps(("Extract", []), ("Load", ["Extract"])));
        var page = Assert.Single(picture.Document.Pages);
        var extract = page.Cells.Single(cell => cell.IsVertex && cell.Value == "Extract");
        var load = page.Cells.Single(cell => cell.IsVertex && cell.Value == "Load");
        var edge = Assert.Single(page.Cells, cell => cell.IsEdge);

        Assert.Equal(extract.Id, edge.SourceId);
        Assert.Equal(load.Id, edge.TargetId);
        Assert.Equal(extract.Geometry!.X + JobWorkflowGraphLayout.NodeWidth, edge.Geometry!.SourcePoint!.X);
        Assert.Equal(load.Geometry!.X, edge.Geometry.TargetPoint!.X);
        Assert.Equal(page.PageWidth, extract.Geometry.X + JobWorkflowGraphLayout.NodeWidth + JobWorkflowGraphLayout.ColumnGap + JobWorkflowGraphLayout.NodeWidth);
        Assert.Empty(picture.Cyclic);
    }

    /// <summary>The arrow into a gated step carries that step's condition, which is what has to be true after the source finishes.</summary>
    [Fact]
    public void Create_Edge_NamesTheConditionThatLetsTheTargetRun()
    {
        var steps = Steps(("Extract", []), ("Load", ["Extract"]));
        steps[1].Condition = JobWorkflowStepCondition.When("Extract", "RowCount", ComparisonOperatorEnum.GreaterThan, "0");

        var edge = Assert.Single(JobWorkflowDiagram.Create("Checkout", steps).Document.Pages[0].Cells, cell => cell.IsEdge);

        Assert.False(string.IsNullOrWhiteSpace(steps[1].ConditionText));
        Assert.Equal(steps[1].ConditionText, edge.Value);
    }

    /// <summary>An ungated step still says so on the arrow, so a plain line is not mistaken for a missing label.</summary>
    [Fact]
    public void Create_Edge_WithoutACondition_SaysAlways()
    {
        var edge = Assert.Single(
            JobWorkflowDiagram.Create("Checkout", Steps(("Extract", []), ("Load", ["Extract"]))).Document.Pages[0].Cells,
            cell => cell.IsEdge);

        Assert.Equal("always", edge.Value);
    }

    [Fact]
    public void Create_Cycle_ReportsTheStepsAndDrawsNoCells()
    {
        var picture = JobWorkflowDiagram.Create("Loop", Steps(("A", ["B"]), ("B", ["A"])));

        Assert.Equal(["A", "B"], [.. picture.Cyclic.Order()]);
        Assert.Empty(picture.Document.Pages[0].Cells);
    }

    private static List<JobWorkflowStepDraft> Steps(params (string Name, string[] DependsOn)[] specs)
    {
        var steps = specs.Select(spec => new JobWorkflowStepDraft { StepName = spec.Name }).ToList();
        foreach (var (step, spec) in steps.Zip(specs))
            step.DependsOn = [.. spec.DependsOn.Select(name => steps.Single(candidate => candidate.StepName == name).Id)];

        return steps;
    }
}
