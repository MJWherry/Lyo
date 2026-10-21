namespace Lyo.Desktop;

/// <summary>Modifier keys on a <see cref="HotkeyChord" />.</summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Ctrl or Control.</summary>
    Ctrl = 1,

    /// <summary>Shift.</summary>
    Shift = 2,

    /// <summary>Alt.</summary>
    Alt = 4,

    /// <summary>Meta, Super, or the Windows key.</summary>
    Meta = 8
}