using Lyo.Exceptions;

namespace Lyo.Desktop;

/// <summary>Lists, finds, and raises top-level windows on Windows and on X11.</summary>
public static class DesktopWindows
{
    /// <summary>Top-level windows. Throws when this desktop cannot list windows.</summary>
    public static IReadOnlyList<DesktopWindow> List()
    {
        if (OperatingSystem.IsWindows())
            return WindowsDesktopWindows.List();

        if (HasX11())
            return X11DesktopWindows.List();

        throw new InvalidOperationException("Window list needs an X11 DISPLAY or Windows.");
    }

    /// <summary>First window whose title contains <paramref name="title" />, ignoring case. A blank title is not a match.</summary>
    public static DesktopWindow? Find(IReadOnlyList<DesktopWindow> windows, string title)
    {
        ArgumentHelpers.ThrowIfNull(windows);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(title);
        foreach (var window in windows) {
            if (string.IsNullOrWhiteSpace(window.Title))
                continue;

            if (window.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
                return window;
        }

        return null;
    }

    /// <summary>Raises the window and asks the desktop to focus it. An id of 0 throws.</summary>
    public static void Activate(long id)
    {
        ArgumentHelpers.ThrowIf(id == 0, "Window id is 0.");
        if (OperatingSystem.IsWindows()) {
            WindowsDesktopWindows.Activate(id);
            return;
        }

        if (HasX11()) {
            X11DesktopWindows.Activate(id);
            return;
        }

        throw new InvalidOperationException("Window activate needs an X11 DISPLAY or Windows.");
    }

    private static bool HasX11() => OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));
}