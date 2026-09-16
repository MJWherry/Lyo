namespace Lyo.Desktop;

/// <summary>Compares an X11 key event with a chord. Caps Lock and Num Lock are ignored so the same shortcut still matches.</summary>
internal static class X11ModifierMatch
{
    internal const uint ShiftMask = 1;
    internal const uint LockMask = 2;
    internal const uint ControlMask = 1 << 2;
    internal const uint Mod1Mask = 1 << 3;
    internal const uint Mod2Mask = 1 << 4;
    internal const uint Mod4Mask = 1 << 6;
    internal const uint Significant = ShiftMask | ControlMask | Mod1Mask | Mod4Mask;

    internal static bool Same(uint eventState, uint chordModifiers) => (eventState & Significant) == (chordModifiers & Significant);
}
