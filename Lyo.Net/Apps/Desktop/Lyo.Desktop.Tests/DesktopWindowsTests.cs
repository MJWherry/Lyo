namespace Lyo.Desktop.Tests;

public class DesktopWindowsTests
{
    [Fact]
    public void Find_Substring_MatchesTitle()
    {
        DesktopWindow[] windows = [
            new(1, "   ", new(0, 0, 10, 10)),
            new(4, "Editor", new(0, 0, 10, 10))
        ];

        var found = DesktopWindows.Find(windows, "edit");

        Assert.NotNull(found);
        Assert.Equal(4L, found.Value.Id);
    }

    [Fact]
    public void Find_DifferentTitle_ReturnsNull()
    {
        DesktopWindow[] windows = [new(4, "Editor", new(0, 0, 10, 10))];
        Assert.Null(DesktopWindows.Find(windows, "notes"));
    }

    [Fact]
    public void Find_BlankTitle_IsNotAMatch()
    {
        DesktopWindow[] windows = [new(1, "   ", new(0, 0, 10, 10))];
        Assert.Null(DesktopWindows.Find(windows, "edit"));
    }

    [Fact]
    public void Activate_ZeroId_Throws()
        => Assert.Throws<ArgumentException>(() => DesktopWindows.Activate(0));
}
