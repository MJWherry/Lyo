using System.Runtime.InteropServices;

namespace Lyo.Desktop;

/// <summary>Plays pointer and key events with XTest.</summary>
internal static partial class X11DesktopInput
{
    internal static void Play(IReadOnlyList<DesktopInputStep> steps)
    {
        GlobalHotkeys.InitializeThreading();
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Could not open the X11 display.");
        try {
            var screen = XDefaultScreen(display);
            foreach (var step in steps)
                PlayOne(display, screen, step);
            XFlush(display);
        }
        finally {
            XCloseDisplay(display);
        }
    }

    private static void PlayOne(IntPtr display, int screen, DesktopInputStep step)
    {
        if (step.Kind == DesktopInputKind.Move) {
            XTestFakeMotionEvent(display, screen, step.X, step.Y, 0);
            return;
        }

        if (step.Kind == DesktopInputKind.Button) {
            XTestFakeButtonEvent(display, (uint)step.Button, step.Down ? 1 : 0, 0);
            return;
        }

        var keysym = Keysym(step);
        var code = XKeysymToKeycode(display, keysym);
        if (code == 0)
            throw new InvalidOperationException($"Unknown key '{step.Key}'.");
        var shift = step.Character && NeedsShift(display, code, keysym);
        if (shift && step.Down)
            XTestFakeKeyEvent(display, ShiftCode(display), 1, 0);
        XTestFakeKeyEvent(display, code, step.Down ? 1 : 0, 0);
        if (shift && !step.Down)
            XTestFakeKeyEvent(display, ShiftCode(display), 0, 0);
    }

    private static ulong Keysym(DesktopInputStep step)
    {
        if (!step.Character)
            return Require(XStringToKeysym(KeysymName(step.Key)), step.Key);
        if (step.Key.Length != 1)
            throw new InvalidOperationException($"Unknown key '{step.Key}'.");
        return step.Key[0];
    }

    private static string KeysymName(string key) => key switch {
        "Control" => "Control_L",
        "Shift" => "Shift_L",
        "Alt" => "Alt_L",
        "Super" => "Super_L",
        _ => key
    };

    private static ulong Require(ulong keysym, string key)
    {
        if (keysym == 0)
            throw new InvalidOperationException($"Unknown key '{key}'.");
        return keysym;
    }

    private static bool NeedsShift(IntPtr display, uint code, ulong keysym)
    {
        if (XkbKeycodeToKeysym(display, code, 0, 0) == keysym)
            return false;
        return XkbKeycodeToKeysym(display, code, 0, 1) == keysym;
    }

    private static uint ShiftCode(IntPtr display)
    {
        var code = XKeysymToKeycode(display, XStringToKeysym("Shift_L"));
        if (code == 0)
            throw new InvalidOperationException("Unknown key 'Shift'.");
        return code;
    }

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XOpenDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XCloseDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XDefaultScreen(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XFlush(IntPtr display);

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial ulong XStringToKeysym(string key);

    [LibraryImport("libX11.so.6")]
    private static partial uint XKeysymToKeycode(IntPtr display, ulong keysym);

    [LibraryImport("libX11.so.6")]
    private static partial ulong XkbKeycodeToKeysym(IntPtr display, uint keycode, int group, int level);

    [LibraryImport("libXtst.so.6")]
    private static partial int XTestFakeMotionEvent(IntPtr display, int screen, int x, int y, ulong delay);

    [LibraryImport("libXtst.so.6")]
    private static partial int XTestFakeButtonEvent(IntPtr display, uint button, int isPress, ulong delay);

    [LibraryImport("libXtst.so.6")]
    private static partial int XTestFakeKeyEvent(IntPtr display, uint keycode, int isPress, ulong delay);
}
