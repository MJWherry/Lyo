namespace Lyo.Desktop.Tests;

public class HotkeyChordTests
{
    [Fact]
    public void Parse_CtrlShiftS_RoundTrips()
    {
        var chord = HotkeyChord.Parse("Ctrl+Shift+S");
        Assert.Equal(HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, chord.Modifiers);
        Assert.Equal("S", chord.Key);
        Assert.Equal("Ctrl+Shift+S", chord.ToString());
    }

    [Fact]
    public void ThrowIfDuplicate_SameChord_Throws()
    {
        var chord = HotkeyChord.Parse("Ctrl+Shift+W");
        var bindings = new HotkeyBinding[] { new(chord, () => { }), new(chord, () => { }) };
        Assert.Throws<InvalidOperationException>(() => HotkeyBinding.ThrowIfDuplicate(bindings));
    }
}