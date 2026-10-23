using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>A modifier plus key stored in a settings file, such as <c>Ctrl+Shift+S</c>. This type does not listen for keys.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct HotkeyChord(HotkeyModifiers Modifiers, string Key)
{
    /// <summary>Parses a chord such as <c>Ctrl+Shift+S</c>. Modifiers may appear in any order. A single-letter key is stored in upper case.</summary>
    public static HotkeyChord Parse(string text)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(text);
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        ArgumentHelpers.ThrowIf(parts.Length == 0, "Hotkey chord is empty.");
        var modifiers = HotkeyModifiers.None;
        string? key = null;
        foreach (var part in parts) {
            if (IsModifier(part, "ctrl", "control"))
                modifiers |= HotkeyModifiers.Ctrl;
            else if (IsModifier(part, "shift"))
                modifiers |= HotkeyModifiers.Shift;
            else if (IsModifier(part, "alt"))
                modifiers |= HotkeyModifiers.Alt;
            else if (IsModifier(part, "meta", "win", "super"))
                modifiers |= HotkeyModifiers.Meta;
            else
                key = part.Length == 1 ? part.ToUpperInvariant() : part;
        }

        ArgumentHelpers.ThrowIfNull(key);
        return new(modifiers, key);
    }

    /// <summary>Formats the chord with modifiers in Ctrl, Shift, Alt, Meta order.</summary>
    public override string ToString()
    {
        var parts = new List<string>(5);
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl))
            parts.Add("Ctrl");

        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
            parts.Add("Shift");

        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
            parts.Add("Alt");

        if (Modifiers.HasFlag(HotkeyModifiers.Meta))
            parts.Add("Meta");

        parts.Add(Key);
        return string.Join('+', parts);
    }

    private static bool IsModifier(string part, params string[] names)
    {
        foreach (var name in names) {
            if (part.Equals(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}