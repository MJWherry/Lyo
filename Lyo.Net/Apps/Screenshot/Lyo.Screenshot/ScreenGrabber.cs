namespace Lyo.Screenshot;

/// <summary>Picks the grabber for this operating system.</summary>
public static class ScreenGrabber
{
    /// <summary>
    /// Windows uses GDI. Linux with <c>DISPLAY</c> uses X11. Wayland with no X display uses <c>grim</c> when it is on <c>PATH</c>. Throws
    /// <see cref="InvalidOperationException" /> when none of those are available.
    /// </summary>
    public static IScreenGrabber Create()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsScreenGrabber();

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")))
            return new X11ScreenGrabber();

        if (GrimScreenGrabber.IsAvailable)
            return new GrimScreenGrabber();

        throw new InvalidOperationException("Screen capture needs an X11 display, Windows, or grim on Wayland.");
    }
}