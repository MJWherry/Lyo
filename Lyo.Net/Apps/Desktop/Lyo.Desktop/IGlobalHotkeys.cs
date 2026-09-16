namespace Lyo.Desktop;

/// <summary>Grabs chords on the desktop so they fire while another window is focused. Call <see cref="GlobalHotkeys.InitializeThreading" /> before any other X11 use on Linux.</summary>
public interface IGlobalHotkeys : IDisposable
{
    /// <summary>Replaces the grabbed chords. An empty list releases every grab. The same chord cannot appear twice.</summary>
    void Bind(IReadOnlyList<HotkeyBinding> bindings);
}
