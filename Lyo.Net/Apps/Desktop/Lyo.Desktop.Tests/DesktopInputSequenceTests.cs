namespace Lyo.Desktop.Tests;

public class DesktopInputSequenceTests
{
    [Fact]
    public void Chord_CtrlA_PressesControlThenKeyThenReleases()
    {
        var steps = DesktopInputSequence.Chord(HotkeyChord.Parse("Ctrl+A"));

        Assert.Equal([
            Step("Control", down: true),
            Step("A", down: true),
            Step("A", down: false),
            Step("Control", down: false)
        ], steps);
    }

    [Fact]
    public void Click_Left_MovesThenPressesThenReleases()
    {
        var steps = DesktopInputSequence.Click(10, 20, DesktopMouseButton.Left);

        Assert.Equal(3, steps.Count);
        Assert.Equal(DesktopInputKind.Move, steps[0].Kind);
        Assert.Equal(10, steps[0].X);
        Assert.Equal(20, steps[0].Y);
        Assert.Equal(DesktopInputKind.Button, steps[1].Kind);
        Assert.Equal(DesktopMouseButton.Left, steps[1].Button);
        Assert.True(steps[1].Down);
        Assert.Equal(DesktopInputKind.Button, steps[2].Kind);
        Assert.Equal(DesktopMouseButton.Left, steps[2].Button);
        Assert.False(steps[2].Down);
    }

    private static DesktopInputStep Step(string key, bool down)
        => new(DesktopInputKind.Key, 0, 0, default, key, down, false);
}
