using Lyo.Desktop;

namespace Lyo.Screenshot;

/// <summary>Drops windows that cannot be picked as a capture target.</summary>
public static class DesktopWindowFilter
{
    /// <summary>Keeps windows with a title and a positive size, and drops the lyo-shot window.</summary>
    public static IReadOnlyList<DesktopWindow> Selectable(IReadOnlyList<DesktopWindow> windows)
    {
        var kept = new List<DesktopWindow>();
        foreach (var window in windows) {
            if (string.IsNullOrWhiteSpace(window.Title) || window.Title == "lyo-shot")
                continue;

            if (window.Bounds.Width < 1 || window.Bounds.Height < 1)
                continue;

            kept.Add(window);
        }

        return kept;
    }

    /// <summary>True when a chosen window id is not among <paramref name="open" />. An id of 0 is not a window selection.</summary>
    public static bool Missing(long windowId, IReadOnlyList<DesktopWindow> open)
    {
        if (windowId == 0)
            return false;

        foreach (var window in open) {
            if (window.Id == windowId)
                return false;
        }

        return true;
    }
}

/// <summary>Lists top-level windows that can be captured. Empty when this platform cannot list windows.</summary>
public static class DesktopWindows
{
    /// <summary>Windows with a title, excluding lyo-shot. Empty when this platform cannot list windows.</summary>
    public static IReadOnlyList<DesktopWindow> List()
    {
        try {
            return DesktopWindowFilter.Selectable(Desktop.DesktopWindows.List());
        }
        catch (Exception ex) when (ex is DllNotFoundException or InvalidOperationException) {
            return [];
        }
    }

    /// <summary>The capture rectangle for a desktop window.</summary>
    public static CaptureRegion Region(DesktopWindow window) => new(window.Bounds.X, window.Bounds.Y, window.Bounds.Width, window.Bounds.Height);
}