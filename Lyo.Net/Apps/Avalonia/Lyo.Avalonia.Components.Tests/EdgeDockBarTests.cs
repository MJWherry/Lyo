using Avalonia.Controls;

namespace Lyo.Desktop.Components.Tests;

public class EdgeDockBarTests
{
    [Fact]
    public void ShouldClose_AfterTheOpenGrace_ClosesWhenThePointerHasLeft() => Assert.True(EdgeDockBar.ShouldClose(false, false, true, 1_000, 500));

    [Fact]
    public void ShouldClose_DuringTheOpenGrace_StaysOpenWhenTheCursorHasNotMoved() => Assert.False(EdgeDockBar.ShouldClose(false, false, true, 200, 500));

    [Fact]
    public void ShouldClose_PointerStillOver_StaysOpen() => Assert.False(EdgeDockBar.ShouldClose(false, true, false, 1_000, 500));

    [Fact]
    public void FromButton_LabelInsideAButton_IsTrue()
    {
        var label = new TextBlock { Text = "Region" };
        _ = new Button { Content = label };
        Assert.True(EdgeDockBar.FromButton(label));
    }

    [Fact]
    public void FromButton_PlainText_IsFalse() => Assert.False(EdgeDockBar.FromButton(new TextBlock { Text = "Region" }));
}