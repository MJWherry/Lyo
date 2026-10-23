using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>Moves the pointer and sends keys. Linux uses XTest. Windows uses <c>SendInput</c>. This is not the AutoIt script language.</summary>
public static class DesktopInput
{
    /// <summary>Moves the pointer to desktop pixel coordinates.</summary>
    public static void Move(int x, int y) => Play([new(DesktopInputKind.Move, x, y, default, "", false, false)]);

    /// <summary>Moves to <paramref name="x" />, <paramref name="y" /> and clicks. The sequence is move, button down, button up.</summary>
    public static void Click(int x, int y, DesktopMouseButton button = DesktopMouseButton.Left) => Play(DesktopInputSequence.Click(x, y, button));

    /// <summary>Types <paramref name="text" /> as characters.</summary>
    public static void Send(string text)
    {
        ArgumentHelpers.ThrowIfNull(text);
        if (text.Length == 0)
            return;

        Play(DesktopInputSequence.Text(text));
    }

    /// <summary>Presses and releases one chord. Modifiers go down first and come up last.</summary>
    public static void Send(HotkeyChord chord) => Play(DesktopInputSequence.Chord(chord));

    private static void Play(IReadOnlyList<DesktopInputStep> steps)
    {
        if (OperatingSystem.IsWindows()) {
            WindowsDesktopInput.Play(steps);
            return;
        }

        if (OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))) {
            X11DesktopInput.Play(steps);
            return;
        }

        throw new InvalidOperationException("Mouse and key input need an X11 DISPLAY or Windows.");
    }
}