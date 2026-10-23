namespace Lyo.Desktop.Tests;

public class X11ModifierMatchTests
{
    [Fact]
    public void Same_CapsLockAndNumLock_StillMatches()
    {
        var chord = X11ModifierMatch.ControlMask | X11ModifierMatch.ShiftMask;
        var state = chord | X11ModifierMatch.LockMask | X11ModifierMatch.Mod2Mask;
        Assert.True(X11ModifierMatch.Same(state, chord));
    }

    [Fact]
    public void Same_DifferentShift_DoesNotMatch()
    {
        var chord = X11ModifierMatch.ControlMask | X11ModifierMatch.ShiftMask;
        Assert.False(X11ModifierMatch.Same(X11ModifierMatch.ControlMask, chord));
    }
}