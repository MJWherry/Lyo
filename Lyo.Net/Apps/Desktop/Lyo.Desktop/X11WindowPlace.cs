using System.Runtime.InteropServices;

namespace Lyo.Desktop;

/// <summary>
/// Moves a mapped window with <c>_NET_MOVERESIZE_WINDOW</c>. Muffin drops a plain configure that shares a turn with a decoration or size change, so the dock bubble would
/// stay where the window already was.
/// </summary>
public static class X11WindowPlace
{
    private static IntPtr _display;

    /// <summary>Asks the window manager to move and, when width or height is positive, resize <paramref name="window" />.</summary>
    public static void TryPlace(IntPtr window, int x, int y, int width, int height) => Send(window, x, y, Math.Max(width, 0), Math.Max(height, 0), width > 0, height > 0);

    /// <summary>True when the window manager still draws a title bar. A missing display counts as present so callers do not hide and show again.</summary>
    public static bool HasTitleBar(IntPtr window)
    {
        if (window == IntPtr.Zero || !OperatingSystem.IsLinux())
            return true;

        var display = Display();
        if (display == IntPtr.Zero)
            return true;

        var atom = XInternAtom(display, "_NET_FRAME_EXTENTS", false);
        if (atom == IntPtr.Zero)
            return true;

        var status = XGetWindowProperty(display, window, atom, 0, 4, false, IntPtr.Zero, out var _, out var format, out var nitems, out var _, out var prop);
        if (status != 0 || prop == IntPtr.Zero || nitems < 4 || format != 32) {
            if (prop != IntPtr.Zero)
                XFree(prop);

            return false;
        }

        try {
            var top = Marshal.ReadIntPtr(prop, 2 * IntPtr.Size).ToInt64();
            return top >= 8;
        }
        finally {
            XFree(prop);
        }
    }

    private static void Send(IntPtr window, int x, int y, int width, int height, bool setWidth, bool setHeight)
    {
        if (window == IntPtr.Zero || !OperatingSystem.IsLinux())
            return;

        var display = Display();
        if (display == IntPtr.Zero)
            return;

        var atom = XInternAtom(display, "_NET_MOVERESIZE_WINDOW", false);
        if (atom == IntPtr.Zero)
            return;

        const int northWest = 1;
        const int sourceApplication = 1;
        var flags = northWest | (1 << 8) | (1 << 9) | (sourceApplication << 12);
        if (setWidth)
            flags |= 1 << 10;

        if (setHeight)
            flags |= 1 << 11;

        var ev = new XEvent {
            Client = new() {
                type = 33,
                send_event = 1,
                display = display,
                window = window,
                message_type = atom,
                format = 32,
                ptr1 = flags,
                ptr2 = x,
                ptr3 = y,
                ptr4 = width,
                ptr5 = height
            }
        };

        const int mask = (1 << 19) | (1 << 20);
        XSendEvent(display, XDefaultRootWindow(display), false, mask, ref ev);
        XFlush(display);
    }

    private static IntPtr Display()
    {
        if (_display == IntPtr.Zero)
            _display = XOpenDisplay(IntPtr.Zero);

        return _display;
    }

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr name);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XInternAtom(IntPtr display, string name, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [DllImport("libX11.so.6")]
    private static extern int XSendEvent(IntPtr display, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool propagate, nint mask, ref XEvent ev);

    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        IntPtr property,
        nint offset,
        nint length,
        [MarshalAs(UnmanagedType.Bool)] bool delete,
        IntPtr type,
        out IntPtr actualType,
        out int actualFormat,
        out nuint nitems,
        out nuint bytesAfter,
        out IntPtr prop);

    [DllImport("libX11.so.6")]
    private static extern int XFree(IntPtr data);

    [StructLayout(LayoutKind.Explicit, Size = 512)]
    private struct XEvent
    {
        [FieldOffset(0)]
        public XClientMessageEvent Client;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XClientMessageEvent
    {
        public int type;
        public IntPtr serial;
        public int send_event;
        public IntPtr display;
        public IntPtr window;
        public IntPtr message_type;
        public int format;
        public nint ptr1;
        public nint ptr2;
        public nint ptr3;
        public nint ptr4;
        public nint ptr5;
    }
}