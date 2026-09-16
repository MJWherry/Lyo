using Lyo.Desktop;

namespace Lyo.Screenshot.Tests;

public class WindowFrameTests
{
    [Fact]
    public void Selectable_DropsBlankTitlesOwnWindowAndEmptyBounds()
    {
        DesktopWindow[] windows = [
            new(1, "  ", new(0, 0, 10, 10)),
            new(2, "lyo-shot", new(0, 0, 10, 10)),
            new(3, "Notes", new(0, 0, 0, 10)),
            new(4, "Editor", new(5, 6, 200, 100))
        ];

        var only = Assert.Single(DesktopWindowFilter.Selectable(windows));
        Assert.Equal(4L, only.Id);
        Assert.Equal("Editor", only.Title);
    }

    [Fact]
    public void Missing_IsTrueOnlyWhenTheChosenWindowIsGone()
    {
        DesktopWindow[] open = [new(4, "Editor", new(0, 0, 10, 10))];
        Assert.False(DesktopWindowFilter.Missing(0, open));
        Assert.False(DesktopWindowFilter.Missing(4, open));
        Assert.True(DesktopWindowFilter.Missing(9, open));
    }

    [Fact]
    public void ClippedTo_DropsThePartAboveTheScreen()
    {
        var clipped = new CaptureRegion(0, -32, 1920, 1192).ClippedTo(new(0, 0, 1920, 1200));
        Assert.Equal(new CaptureRegion(0, 0, 1920, 1160), clipped);
    }

    [Fact]
    public void ClippedTo_DropsARectangleThatMissesTheScreen()
    {
        var clipped = new CaptureRegion(2000, 10, 100, 80).ClippedTo(new(0, 0, 1920, 1200));
        Assert.Equal(0, clipped.Width);
        Assert.Equal(0, clipped.Height);
    }
}
