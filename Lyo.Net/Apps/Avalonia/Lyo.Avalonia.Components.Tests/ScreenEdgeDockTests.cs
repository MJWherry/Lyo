namespace Lyo.Desktop.Components.Tests;

public class ScreenEdgeDockTests
{
    [Fact]
    public void Place_CloserToRight_HugsRightEdge()
    {
        var anchor = ScreenEdgeDock.Place(0, 0, 1920, 1080, 1700, 340, 200, 400, 28, 56);
        Assert.Equal(ScreenEdge.Right, anchor.Edge);
        Assert.Equal(1920 - 28, anchor.X);
        Assert.Equal(540 - (56 / 2), anchor.Y);
        Assert.Equal(28, anchor.Width);
        Assert.Equal(56, anchor.Height);
    }

    [Fact]
    public void Place_CloserToLeft_HugsLeftEdge()
    {
        var anchor = ScreenEdgeDock.Place(80, 10, 1600, 900, 90, 400, 40, 80, 28, 56);
        Assert.Equal(ScreenEdge.Left, anchor.Edge);
        Assert.Equal(80, anchor.X);
    }

    [Fact]
    public void Place_CloserToTop_HugsTopEdge()
    {
        var anchor = ScreenEdgeDock.Place(0, 0, 1920, 1080, 800, 10, 320, 40, 28, 56);
        Assert.Equal(ScreenEdge.Top, anchor.Edge);
        Assert.Equal(0, anchor.Y);
        Assert.Equal(960 - (56 / 2), anchor.X);
        Assert.Equal(56, anchor.Width);
        Assert.Equal(28, anchor.Height);
    }

    [Fact]
    public void Place_CloserToBottom_HugsBottomEdge()
    {
        var anchor = ScreenEdgeDock.Place(0, 0, 1920, 1080, 800, 1000, 320, 40, 28, 56);
        Assert.Equal(ScreenEdge.Bottom, anchor.Edge);
        Assert.Equal(1080 - 28, anchor.Y);
        Assert.Equal(960 - (56 / 2), anchor.X);
    }

    [Fact]
    public void Place_LeftEdgeNearBottom_ClampsBubbleInsideWorkArea()
    {
        var anchor = ScreenEdgeDock.Place(0, 0, 1920, 1080, 0, 1040, 20, 40, 28, 56);
        Assert.Equal(ScreenEdge.Left, anchor.Edge);
        Assert.Equal(1080 - 56, anchor.Y);
        Assert.InRange(anchor.Y, 0, 1080 - 56);
    }

    [Fact]
    public void Expand_RightEdge_GrowsInward()
    {
        var collapsed = new ScreenEdgeAnchor(ScreenEdge.Right, 1892, 100, 28, 56);
        var open = ScreenEdgeDock.Expand(collapsed, 200);
        Assert.Equal(1692, open.X);
        Assert.Equal(228, open.Width);
        Assert.Equal(100, open.Y);
        Assert.Equal(56, open.Height);
    }

    [Fact]
    public void Clamp_TopNearTheRight_StaysOnTheTopAndInsideTheWorkArea()
    {
        var open = new ScreenEdgeAnchor(ScreenEdge.Top, 1800, 0, 240, 48);
        var clamped = ScreenEdgeDock.Clamp(open, 0, 0, 1920, 1080);
        Assert.Equal(ScreenEdge.Top, clamped.Edge);
        Assert.Equal(0, clamped.Y);
        Assert.Equal(240, clamped.Width);
        Assert.Equal(1920, clamped.X + clamped.Width);
    }

    [Fact]
    public void Clamp_RightEdgeNearTheBottom_StaysOnTheRight()
    {
        var open = new ScreenEdgeAnchor(ScreenEdge.Right, 1600, 1000, 220, 90);
        var clamped = ScreenEdgeDock.Clamp(open, 0, 0, 1920, 1080);
        Assert.Equal(1920, clamped.X + clamped.Width);
        Assert.Equal(1080 - 90, clamped.Y);
    }

    [Fact]
    public void IconBias_MiddleOfTheEdge_StaysCentered()
        => Assert.Equal(0, ScreenEdgeDock.IconBias(400, 400, 56, 14));

    [Fact]
    public void IconBias_TurnFromTopToRight_KeepsTheIconOnTheSamePoint()
    {
        const int thickness = 28;
        const int diameter = 56;
        const int width = 1920;
        const int height = 1080;
        var max = (diameter - thickness) / 2d;
        var top = ScreenEdgeDock.Place(0, 0, width, height, 1900, 8, 1, 1, thickness, diameter);
        var right = ScreenEdgeDock.Place(0, 0, width, height, 1912, 16, 1, 1, thickness, diameter);
        Assert.Equal(ScreenEdge.Top, top.Edge);
        Assert.Equal(ScreenEdge.Right, right.Edge);
        var topBias = ScreenEdgeDock.IconBias(top.X, width - top.X - top.Width, diameter, max);
        var rightBias = ScreenEdgeDock.IconBias(right.Y, height - right.Y - right.Height, diameter, max);
        var topIconX = top.X + (top.Width / 2d) + topBias;
        var topIconY = top.Y + (top.Height / 2d);
        var rightIconX = right.X + (right.Width / 2d);
        var rightIconY = right.Y + (right.Height / 2d) + rightBias;
        Assert.Equal(topIconX, rightIconX);
        Assert.Equal(topIconY, rightIconY);
    }

    [Fact]
    public void Fit_BottomEdge_GrowsAlongTheEdgeAndInward()
    {
        var collapsed = new ScreenEdgeAnchor(ScreenEdge.Bottom, 100, 1000, 56, 28);
        var open = ScreenEdgeDock.Fit(collapsed, 240, 90);
        Assert.Equal(100, open.X);
        Assert.Equal(938, open.Y);
        Assert.Equal(240, open.Width);
        Assert.Equal(90, open.Height);
    }

    [Fact]
    public void PulledOff_RightEdge_NeedsThePointerInwardOfTheThreshold()
    {
        Assert.False(ScreenEdgeDock.PulledOff(ScreenEdge.Right, 0, 0, 1920, 1080, 1900, 400, ScreenEdgeDock.OffEdge));
        Assert.True(ScreenEdgeDock.PulledOff(ScreenEdge.Right, 0, 0, 1920, 1080, 1800, 400, ScreenEdgeDock.OffEdge));
    }

    [Fact]
    public void AwayFromEdges_NearASide_StaysDocked()
    {
        Assert.False(ScreenEdgeDock.AwayFromEdges(0, 0, 1920, 1080, 10, 400, ScreenEdgeDock.OffEdge));
        Assert.True(ScreenEdgeDock.AwayFromEdges(0, 0, 1920, 1080, 400, 400, ScreenEdgeDock.OffEdge));
    }

    [Fact]
    public void Gesture_SmallRelease_IsAClick_AndASecondOne_IsADoubleClick()
    {
        var gesture = new EdgeDockGesture();
        gesture.Press(10, 10);
        gesture.Move(12, 11);
        Assert.Equal(EdgeDockTap.Click, gesture.Release(12, 11, 1_000));
        gesture.Press(12, 11);
        Assert.Equal(EdgeDockTap.DoubleClick, gesture.Release(12, 11, 1_200));
    }

    [Fact]
    public void Gesture_LeavingTheSlopBox_IsADrag()
    {
        var gesture = new EdgeDockGesture();
        gesture.Press(0, 0);
        Assert.True(gesture.Move(20, 0));
        Assert.Equal(EdgeDockTap.Drag, gesture.Release(20, 0, 1_000));
    }

    [Fact]
    public void Corners_LeftEdge_RoundsTheOuterSide()
    {
        var corners = ScreenEdgeDock.Corners(ScreenEdge.Left);
        Assert.Equal(0, corners.TopLeft);
        Assert.Equal(ScreenEdgeDock.Thickness, corners.TopRight);
        Assert.Equal(ScreenEdgeDock.Thickness, corners.BottomRight);
        Assert.Equal(0, corners.BottomLeft);
    }
}
