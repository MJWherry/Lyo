namespace Lyo.Desktop.Components.Tests;

public class ToolDockTests
{
    [Fact]
    public void Move_ShowsThePaneOnTheNewEdge()
    {
        var layout = ToolDockState.Create(
            ("tree", "Tree", ToolDockEdge.Left, true), ("metadata", "Metadata", ToolDockEdge.Right, true), ("log", "Log", ToolDockEdge.Bottom, false));

        layout.Move("log", ToolDockEdge.Right);
        Assert.Equal(ToolDockEdge.Right, layout.Edge("log"));
        Assert.True(layout.IsVisible("log"));
        Assert.Equal("Log", layout.Pane("log").Title);
        Assert.Equal(3, layout.Panes.Count);
    }

    [Fact]
    public void EdgeAt_BottomBandWinsOverTheCorner()
    {
        Assert.Equal(ToolDockEdge.Bottom, ToolDockHit.EdgeAt(10, 560, 800, 600));
        Assert.Equal(ToolDockEdge.Left, ToolDockHit.EdgeAt(10, 100, 800, 600));
        Assert.Equal(ToolDockEdge.Right, ToolDockHit.EdgeAt(760, 100, 800, 600));
        Assert.Null(ToolDockHit.EdgeAt(400, 300, 800, 600));
    }

    [Fact]
    public void HugsHeader_MinimizedLog_CollapsesTheEdge()
    {
        var layout = ToolDockState.Create(("log", "Log", ToolDockEdge.Bottom, true));
        Assert.False(layout.HugsHeader(ToolDockEdge.Bottom));
        layout.ToggleMinimized("log");
        Assert.True(layout.Pane("log").Minimized);
        Assert.True(layout.HugsHeader(ToolDockEdge.Bottom));
        layout.ToggleMinimized("log");
        Assert.False(layout.HugsHeader(ToolDockEdge.Bottom));
    }

    [Fact]
    public void HugsHeader_HiddenLog_DoesNotClaimTheEdge()
    {
        var layout = ToolDockState.Create(("log", "Log", ToolDockEdge.Bottom, true));
        layout.SetVisible("log", false);
        Assert.False(layout.HugsHeader(ToolDockEdge.Bottom));
    }

    [Fact]
    public void HugsHeader_OneOpenPane_KeepsTheEdgeOpen()
    {
        var layout = ToolDockState.Create(("log", "Log", ToolDockEdge.Bottom, true), ("output", "Output", ToolDockEdge.Bottom, true));
        layout.ToggleMinimized("log");
        Assert.False(layout.HugsHeader(ToolDockEdge.Bottom));
    }
}