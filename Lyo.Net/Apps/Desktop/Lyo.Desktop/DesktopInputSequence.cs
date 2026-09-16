namespace Lyo.Desktop;

internal enum DesktopInputKind
{
    Move,
    Button,
    Key
}

internal readonly record struct DesktopInputStep(DesktopInputKind Kind, int X, int Y, DesktopMouseButton Button, string Key, bool Down, bool Character);

/// <summary>Builds the pointer and key events. Platforms play the list.</summary>
internal static class DesktopInputSequence
{
    internal static IReadOnlyList<DesktopInputStep> Click(int x, int y, DesktopMouseButton button) => [
        new(DesktopInputKind.Move, x, y, button, "", false, false),
        new(DesktopInputKind.Button, x, y, button, "", true, false),
        new(DesktopInputKind.Button, x, y, button, "", false, false)
    ];

    internal static IReadOnlyList<DesktopInputStep> Chord(HotkeyChord chord)
    {
        var mods = ModifierNames(chord.Modifiers);
        var steps = new List<DesktopInputStep>(mods.Count * 2 + 2);
        foreach (var name in mods)
            steps.Add(Key(name, down: true));
        steps.Add(Key(chord.Key, down: true));
        steps.Add(Key(chord.Key, down: false));
        for (var i = mods.Count - 1; i >= 0; i--)
            steps.Add(Key(mods[i], down: false));
        return steps;
    }

    internal static IReadOnlyList<DesktopInputStep> Text(string text)
    {
        var steps = new List<DesktopInputStep>(text.Length * 2);
        foreach (var ch in text) {
            var key = ch.ToString();
            steps.Add(new(DesktopInputKind.Key, 0, 0, default, key, true, true));
            steps.Add(new(DesktopInputKind.Key, 0, 0, default, key, false, true));
        }

        return steps;
    }

    private static DesktopInputStep Key(string key, bool down) => new(DesktopInputKind.Key, 0, 0, default, key, down, false);

    private static List<string> ModifierNames(HotkeyModifiers mods)
    {
        var names = new List<string>(4);
        if (mods.HasFlag(HotkeyModifiers.Ctrl))
            names.Add("Control");
        if (mods.HasFlag(HotkeyModifiers.Shift))
            names.Add("Shift");
        if (mods.HasFlag(HotkeyModifiers.Alt))
            names.Add("Alt");
        if (mods.HasFlag(HotkeyModifiers.Meta))
            names.Add("Super");
        return names;
    }
}
