using System.Runtime.InteropServices;
using System.Text;

namespace Lyo.Desktop;

/// <summary>Reads <c>_NET_CLIENT_LIST</c> and raises a window with <c>_NET_ACTIVE_WINDOW</c>.</summary>
internal static partial class X11DesktopWindows
{
    private const int ClientMessage = 33;
    private const nint ActiveMask = (1 << 20) | (1 << 19);

    internal static IReadOnlyList<DesktopWindow> List()
    {
        GlobalHotkeys.InitializeThreading();
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Could not open the X11 display.");

        try {
            var root = XRootWindow(display, XDefaultScreen(display));
            var ids = WindowIds(display, root);
            var windows = new List<DesktopWindow>(ids.Length);
            foreach (var id in ids) {
                if (TryDescribe(display, root, id, out var window))
                    windows.Add(window);
            }

            return windows;
        }
        finally {
            XSync(display, 0);
            XCloseDisplay(display);
        }
    }

    internal static void Activate(long id)
    {
        GlobalHotkeys.InitializeThreading();
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero)
            throw new InvalidOperationException("Could not open the X11 display.");

        try {
            var root = XRootWindow(display, XDefaultScreen(display));
            var window = new IntPtr(id);
            var atom = XInternAtom(display, "_NET_ACTIVE_WINDOW", 0);
            SendActive(display, root, window, atom);
            XRaiseWindow(display, window);
            XFlush(display);
        }
        finally {
            XCloseDisplay(display);
        }
    }

    private static void SendActive(IntPtr display, IntPtr root, IntPtr window, ulong atom)
    {
        var ev = Marshal.AllocHGlobal(192);
        try {
            var zeros = new byte[192];
            Marshal.Copy(zeros, 0, ev, zeros.Length);
            Marshal.WriteInt32(ev, 0, ClientMessage);
            Marshal.WriteIntPtr(ev, 32, window);
            Marshal.WriteIntPtr(ev, 40, (IntPtr)(long)atom);
            Marshal.WriteInt32(ev, 48, 32);
            Marshal.WriteIntPtr(ev, 56, 1);
            XSendEvent(display, root, 0, ActiveMask, ev);
        }
        finally {
            Marshal.FreeHGlobal(ev);
        }
    }

    private static long[] WindowIds(IntPtr display, IntPtr root)
    {
        if (!TryProperty(display, root, "_NET_CLIENT_LIST", out var data, out var format, out var count))
            return [];

        try {
            if (format != 32)
                return [];

            var ids = new long[count];
            for (var i = 0; i < count; i++)
                ids[i] = Marshal.ReadIntPtr(data, i * IntPtr.Size).ToInt64();

            return ids;
        }
        finally {
            XFree(data);
        }
    }

    private static bool TryDescribe(IntPtr display, IntPtr root, long id, out DesktopWindow window)
    {
        window = default;
        var handle = new IntPtr(id);
        var title = Title(display, handle);
        if (XGetGeometry(display, handle, out var _, out var _, out var _, out var width, out var height, out var _, out var _) == 0)
            return false;

        if (XTranslateCoordinates(display, handle, root, 0, 0, out var x, out var y, out var _) == 0)
            return false;

        var bounds = new DesktopRect(x, y, (int)width, (int)height);
        if (TryExtents(display, handle, out var left, out var right, out var top, out var bottom))
            bounds = WindowFrame.Outer(bounds, left, right, top, bottom);

        window = new(id, title, bounds);
        return true;
    }

    private static string Title(IntPtr display, IntPtr window)
    {
        var utf8 = PropertyText(display, window, "_NET_WM_NAME");
        return string.IsNullOrWhiteSpace(utf8) ? PropertyText(display, window, "WM_NAME") : utf8;
    }

    private static string PropertyText(IntPtr display, IntPtr window, string name)
    {
        if (!TryProperty(display, window, name, out var data, out var format, out var count) || format != 8)
            return "";

        try {
            var bytes = new byte[count];
            Marshal.Copy(data, bytes, 0, count);
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0').Trim();
        }
        finally {
            XFree(data);
        }
    }

    private static bool TryExtents(IntPtr display, IntPtr window, out int left, out int right, out int top, out int bottom)
    {
        left = right = top = bottom = 0;
        if (!TryProperty(display, window, "_NET_FRAME_EXTENTS", out var data, out var format, out var count))
            return false;

        try {
            if (format != 32 || count < 4)
                return false;

            left = (int)Marshal.ReadIntPtr(data, 0 * IntPtr.Size);
            right = (int)Marshal.ReadIntPtr(data, 1 * IntPtr.Size);
            top = (int)Marshal.ReadIntPtr(data, 2 * IntPtr.Size);
            bottom = (int)Marshal.ReadIntPtr(data, 3 * IntPtr.Size);
            return true;
        }
        finally {
            XFree(data);
        }
    }

    private static bool TryProperty(IntPtr display, IntPtr window, string name, out IntPtr data, out int format, out int count)
    {
        data = IntPtr.Zero;
        format = 0;
        count = 0;
        var atom = XInternAtom(display, name, 1);
        if (atom == 0)
            return false;

        if (XGetWindowProperty(display, window, atom, 0, 1024, 0, 0, out var _, out format, out var items, out var _, out data) != 0 || data == IntPtr.Zero) {
            if (data != IntPtr.Zero)
                XFree(data);

            return false;
        }

        count = items > 512 ? 512 : (int)items;
        return count > 0;
    }

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XOpenDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XCloseDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XDefaultScreen(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XRootWindow(IntPtr display, int screen);

    [LibraryImport("libX11.so.6")]
    private static partial int XSync(IntPtr display, int discard);

    [LibraryImport("libX11.so.6")]
    private static partial int XFlush(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XFree(IntPtr data);

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial ulong XInternAtom(IntPtr display, string name, int onlyIfExists);

    [LibraryImport("libX11.so.6")]
    private static partial int XSendEvent(IntPtr display, IntPtr window, int propagate, nint mask, IntPtr ev);

    [LibraryImport("libX11.so.6")]
    private static partial int XRaiseWindow(IntPtr display, IntPtr window);

    [LibraryImport("libX11.so.6")]
    private static partial int XGetWindowProperty(
        IntPtr display,
        IntPtr window,
        ulong property,
        long longOffset,
        long longLength,
        int delete,
        ulong reqType,
        out ulong actualType,
        out int actualFormat,
        out ulong nitems,
        out ulong bytesAfter,
        out IntPtr prop);

    [LibraryImport("libX11.so.6")]
    private static partial int XGetGeometry(
        IntPtr display,
        IntPtr drawable,
        out IntPtr root,
        out int x,
        out int y,
        out uint width,
        out uint height,
        out uint border,
        out uint depth);

    [LibraryImport("libX11.so.6")]
    private static partial int XTranslateCoordinates(
        IntPtr display,
        IntPtr source,
        IntPtr destination,
        int sourceX,
        int sourceY,
        out int destinationX,
        out int destinationY,
        out IntPtr child);
}