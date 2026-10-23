namespace Lyo.Desktop.Tests;

public class WindowFrameTests
{
    [Fact]
    public void Outer_AddsFrameOnEachEdge()
    {
        var outer = WindowFrame.Outer(new(10, 30, 100, 80), 4, 6, 28, 2);
        Assert.Equal(new(6, 2, 110, 110), outer);
    }

    [Fact]
    public void Outer_NegativeExtents_CountAsZero()
    {
        var outer = WindowFrame.Outer(new(10, 30, 100, 80), -1, -2, -3, -4);
        Assert.Equal(new(10, 30, 100, 80), outer);
    }
}