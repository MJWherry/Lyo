using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>One grabbed chord and the handler that runs when it is pressed. The handler is called on the hotkey thread.</summary>
public sealed class HotkeyBinding
{
    /// <summary>Chord that is grabbed.</summary>
    public HotkeyChord Chord { get; }

    /// <summary>Called when <see cref="Chord" /> is pressed.</summary>
    public Action Handler { get; }

    /// <summary>Creates a binding. <paramref name="handler" /> runs on the hotkey thread, so it should hop to the UI thread before touching controls.</summary>
    public HotkeyBinding(HotkeyChord chord, Action handler)
    {
        ArgumentHelpers.ThrowIfNull(handler);
        Chord = chord;
        Handler = handler;
    }

    /// <summary>Throws when the same chord appears twice. <see cref="IGlobalHotkeys.Bind" /> calls this before it grabs.</summary>
    public static void ThrowIfDuplicate(IReadOnlyList<HotkeyBinding> bindings)
    {
        ArgumentHelpers.ThrowIfNull(bindings);
        var seen = new HashSet<HotkeyChord>();
        foreach (var binding in bindings) {
            ArgumentHelpers.ThrowIfNull(binding);
            if (!seen.Add(binding.Chord))
                throw new InvalidOperationException("That shortcut is already in use.");
        }
    }
}