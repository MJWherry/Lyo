using Lyo.Job.Web.Components;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>
/// The graph draws nodes at absolute coordinates and arrows as precomputed SVG paths, so the layout is pure arithmetic rather than something only a browser can check.
/// These lock the stage assignment and the edge anchoring that arithmetic produces.
/// </summary>
public class JobWorkflowGraphLayoutTests
{
    [Fact]
    public void Build_Chain_PlacesEachStepInItsOwnColumn()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(("Extract", []), ("Transform", ["Extract"]), ("Load", ["Transform"])));

        Assert.Equal(3, layout.Nodes.Count);
        Assert.Equal([0, 1, 2], [.. layout.Nodes.OrderBy(n => n.Column).Select(n => n.Column)]);
        Assert.All(layout.Nodes, n => Assert.Equal(0, n.Row));
        Assert.Equal(2, layout.Edges.Count);
    }

    /// <summary>Two reports running in parallel and an email step joining them: the shape the builder has to be able to draw.</summary>
    [Fact]
    public void Build_FanOutThenFanIn_StacksSiblingsInOneColumn()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(
            ("Extract", []), ("ReportA", ["Extract"]), ("ReportB", ["Extract"]), ("Email", ["ReportA", "ReportB"])));

        Assert.Equal(1, layout.Nodes.Count(n => n.Column == 0));
        Assert.Equal(2, layout.Nodes.Count(n => n.Column == 1));
        Assert.Equal(1, layout.Nodes.Count(n => n.Column == 2));
        Assert.Equal([0, 1], [.. layout.Nodes.Where(n => n.Column == 1).Select(n => n.Row).Order()]);
        Assert.Equal(4, layout.Edges.Count);
    }

    /// <summary>Siblings in one column are stacked, not overlaid. A shared Y would draw both reports on top of each other.</summary>
    [Fact]
    public void Build_SiblingsInOneColumn_DoNotShareAY()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(("Extract", []), ("ReportA", ["Extract"]), ("ReportB", ["Extract"])));

        var siblings = layout.Nodes.Where(n => n.Column == 1).ToList();
        Assert.Equal(2, siblings.Count);
        Assert.NotEqual(siblings[0].Y, siblings[1].Y);
    }

    [Fact]
    public void Build_Cycle_ReportsTheStepsAndDrawsNoEdgesIntoThem()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(("A", ["B"]), ("B", ["A"])));

        Assert.Equal(["A", "B"], [.. layout.Cyclic.Order()]);
        Assert.Empty(layout.Nodes);
        Assert.Empty(layout.Edges);
    }

    /// <summary>
    /// The stroke has to meet both borders. A cubic that ends at the right coordinates still bows off the node when the cards are not the same height, so the path is an
    /// orthogonal polyline whose last point is the target's left edge.
    /// </summary>
    [Fact]
    public void Build_Edge_StartsOnSourceRightAndEndsOnTargetLeft()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(("Extract", []), ("Load", ["Extract"])));

        var source = layout.Nodes.Single(n => n.Name == "Extract");
        var target = layout.Nodes.Single(n => n.Name == "Load");
        var edge = Assert.Single(layout.Edges);

        Assert.Equal(source.X + JobWorkflowGraphLayout.NodeWidth, edge.StartX);
        Assert.Equal(target.X, edge.EndX);
        Assert.InRange(edge.StartY, source.Y, source.Y + JobWorkflowGraphLayout.NodeHeight);
        Assert.InRange(edge.EndY, target.Y, target.Y + JobWorkflowGraphLayout.NodeHeight);
        Assert.DoesNotContain("C ", edge.Path);
        Assert.EndsWith($"{edge.EndX} {edge.EndY}", edge.Path);
    }

    /// <summary>Two arrows into one step that share a point draw on top of each other. Each lands on the target's left edge at its own height.</summary>
    [Fact]
    public void Build_TwoIncomingEdges_DoNotShareAnEndPoint()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(
            ("Extract", []), ("ReportA", ["Extract"]), ("ReportB", ["Extract"]), ("Email", ["ReportA", "ReportB"])));

        var email = layout.Nodes.Single(n => n.Name == "Email");
        var incoming = layout.Edges.Where(e => e.ToName == "Email").ToList();

        Assert.Equal(2, incoming.Count);
        Assert.All(incoming, e => Assert.Equal(email.X, e.EndX));
        Assert.All(incoming, e => Assert.InRange(e.EndY, email.Y, email.Y + JobWorkflowGraphLayout.NodeHeight));
        Assert.NotEqual(incoming[0].EndY, incoming[1].EndY);
    }

    /// <summary>The container is absolutely sized from the layout, so it has to cover the furthest node rather than clipping it.</summary>
    [Fact]
    public void Build_Canvas_CoversEveryNode()
    {
        var layout = JobWorkflowGraphLayout.Build(Steps(("Extract", []), ("ReportA", ["Extract"]), ("ReportB", ["Extract"])));

        Assert.Equal(layout.Nodes.Max(n => n.X) + JobWorkflowGraphLayout.NodeWidth, layout.Width);
        Assert.Equal(layout.Nodes.Max(n => n.Y) + JobWorkflowGraphLayout.NodeHeight, layout.Height);
    }

    [Fact]
    public void Build_NoSteps_ProducesAnEmptyLayout()
    {
        var layout = JobWorkflowGraphLayout.Build([]);

        Assert.Empty(layout.Nodes);
        Assert.Empty(layout.Edges);
        Assert.Equal(0, layout.Width);
    }

    /// <summary>A dependency naming a step that does not exist must not invent a node or an edge; the builder reports it, the graph just omits it.</summary>
    [Fact]
    public void Build_UnknownDependency_IsIgnored()
    {
        var layout = JobWorkflowGraphLayout.Build([new JobWorkflowStepDraft { StepName = "Load", DependsOn = [Guid.NewGuid()] }]);

        Assert.Single(layout.Nodes);
        Assert.Empty(layout.Edges);
        Assert.Equal(0, layout.Nodes[0].Column);
    }

    /// <summary>Builds steps whose dependencies are the draft ids of the named siblings, which is how the editor stores a link.</summary>
    private static List<JobWorkflowStepDraft> Steps(params (string Name, string[] DependsOn)[] specs)
    {
        var steps = specs.Select(spec => new JobWorkflowStepDraft { StepName = spec.Name }).ToList();
        foreach (var (step, spec) in steps.Zip(specs))
            step.DependsOn = [.. spec.DependsOn.Select(name => steps.Single(s => s.StepName == name).Id)];

        return steps;
    }
}
