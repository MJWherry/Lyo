namespace Lyo.Story.Web.Components.Tests;

public class StoryGraphLayoutTests
{
    [Fact]
    public void Build_HeistShape_PlacesStartThenBranches()
    {
        var layout = StoryGraphLayout.Build(StoryDraftFixtures.HeistShape());
        var start = layout.Nodes.Single(node => node.Key == "vault_door");
        var inside = layout.Nodes.Single(node => node.Key == "inside");
        Assert.Equal(0, start.Column);
        Assert.Equal(start.Column + 1, inside.Column);
        Assert.Equal(start.X + StoryGraphLayout.NodeWidth + StoryGraphLayout.ColumnGap, inside.X);
        Assert.Contains(layout.Nodes, node => node.Key == "loose");
    }

    [Fact]
    public void Build_Cycle_ReportsNodesAndOmitsThem()
    {
        var layout = StoryGraphLayout.Build(StoryDraftFixtures.Cycle());
        Assert.Equal(["a", "b"], layout.Cyclic.Order());
        Assert.Empty(layout.Nodes);
        Assert.Empty(layout.Edges);
    }
}