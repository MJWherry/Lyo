namespace Lyo.Desktop;

/// <summary>Creates the platform hotkey grabber. Linux uses X11. Windows uses <c>RegisterHotKey</c>.</summary>
public static class GlobalHotkeys
{
    /// <summary>Call before any other X11 use, including Avalonia startup. Xlib is not thread-safe until this runs. On Windows this does nothing.</summary>
    public static void InitializeThreading() => X11GlobalHotkeys.InitializeThreading();

    /// <summary>Starts the grabber thread. Bind chords with <see cref="IGlobalHotkeys.Bind" />.</summary>
    public static IGlobalHotkeys Create() => OperatingSystem.IsWindows() ? new WindowsGlobalHotkeys() : new X11GlobalHotkeys();
}