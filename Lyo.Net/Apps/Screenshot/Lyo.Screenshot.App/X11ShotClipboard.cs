using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Lyo.Screenshot.App;

/// <summary>Owns the X11 clipboard with both the image and the file, so a paste can land the picture or the file.</summary>
internal static partial class X11ShotClipboard
{
    private const int SelectionClear = 29;
    private const int SelectionRequest = 30;
    private const int SelectionNotify = 31;
    private const int PropModeReplace = 0;
    private static readonly ConcurrentQueue<Offer> Offers = new();
    private static readonly AutoResetEvent Pulse = new(false);
    private static readonly Lock Gate = new();
    private static string _path = "";
    private static byte[] _png = [];
    private static bool _started;

    public static bool TryOffer(string path, byte[] png)
    {
        if (!OperatingSystem.IsLinux() || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            return false;

        EnsureStarted();
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Offers.Enqueue(new(path, png, ready));
        Pulse.Set();
        return ready.Task.Wait(TimeSpan.FromSeconds(2)) && ready.Task.Result;
    }

    private static void EnsureStarted()
    {
        lock (Gate) {
            if (_started)
                return;

            _started = true;
            var thread = new Thread(Loop) { IsBackground = true, Name = "lyo-shot-clipboard" };
            thread.Start();
        }
    }

    private static void Loop()
    {
        XInitThreads();
        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero) {
            FailPending();
            return;
        }

        var screen = XDefaultScreen(display);
        var root = XRootWindow(display, screen);
        var window = XCreateSimpleWindow(display, root, -10, -10, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
        var clipboard = Intern(display, "CLIPBOARD");
        var targets = Intern(display, "TARGETS");
        var pngAtom = Intern(display, "image/png");
        var uriList = Intern(display, "text/uri-list");
        var gnome = Intern(display, "x-special/gnome-copied-files");
        var utf8 = Intern(display, "UTF8_STRING");
        var plain = Intern(display, "text/plain");
        var atomType = Intern(display, "ATOM");
        var maxBytes = Math.Max(0, ((int)XMaxRequestSize(display) - 32) * 4);
        var owned = false;
        while (true) {
            while (Offers.TryDequeue(out var offer)) {
                _path = offer.Path;
                _png = offer.Png;
                if (!owned) {
                    XSetSelectionOwner(display, clipboard, window, IntPtr.Zero);
                    XFlush(display);
                    owned = XGetSelectionOwner(display, clipboard) == window;
                }

                offer.Ready.TrySetResult(owned);
            }

            while (XPending(display) > 0) {
                var ev = Marshal.AllocHGlobal(256);
                try {
                    XNextEvent(display, ev);
                    var type = Marshal.ReadInt32(ev);
                    if (type == SelectionClear)
                        owned = false;
                    else if (type == SelectionRequest)
                        Answer(display, ev, targets, pngAtom, uriList, gnome, utf8, plain, atomType, maxBytes);
                }
                finally {
                    Marshal.FreeHGlobal(ev);
                }
            }

            Pulse.WaitOne(50);
        }
    }

    private static void FailPending()
    {
        while (Offers.TryDequeue(out var offer))
            offer.Ready.TrySetResult(false);
    }

    private static void Answer(IntPtr display, IntPtr ev, IntPtr targets, IntPtr pngAtom, IntPtr uriList, IntPtr gnome, IntPtr utf8, IntPtr plain, IntPtr atomType, int maxBytes)
    {
        var requestor = Marshal.ReadIntPtr(ev, 40);
        var selection = Marshal.ReadIntPtr(ev, 48);
        var target = Marshal.ReadIntPtr(ev, 56);
        var property = Marshal.ReadIntPtr(ev, 64);
        var time = Marshal.ReadIntPtr(ev, 72);
        if (property == IntPtr.Zero)
            property = target;

        var delivered = property;
        if (target == targets)
            WriteAtoms(display, requestor, property, atomType, [targets, pngAtom, uriList, gnome, utf8, plain]);
        else if (target == pngAtom && _png.Length > 0 && _png.Length <= maxBytes)
            WriteBytes(display, requestor, property, pngAtom, _png);
        else if (target == uriList)
            WriteBytes(display, requestor, property, uriList, UriList(_path));
        else if (target == gnome)
            WriteBytes(display, requestor, property, gnome, GnomeList(_path));
        else if (target == utf8 || target == plain)
            WriteBytes(display, requestor, property, utf8, Encoding.UTF8.GetBytes(_path));
        else
            delivered = IntPtr.Zero;

        var notify = Marshal.AllocHGlobal(256);
        try {
            for (var i = 0; i < 256; i++)
                Marshal.WriteByte(notify, i, 0);

            Marshal.WriteInt32(notify, SelectionNotify);
            Marshal.WriteInt32(notify, 16, 1);
            Marshal.WriteIntPtr(notify, 24, display);
            Marshal.WriteIntPtr(notify, 32, requestor);
            Marshal.WriteIntPtr(notify, 40, selection);
            Marshal.WriteIntPtr(notify, 48, target);
            Marshal.WriteIntPtr(notify, 56, delivered);
            Marshal.WriteIntPtr(notify, 64, time);
            XSendEvent(display, requestor, 0, IntPtr.Zero, notify);
            XFlush(display);
        }
        finally {
            Marshal.FreeHGlobal(notify);
        }
    }

    private static void WriteAtoms(IntPtr display, IntPtr window, IntPtr property, IntPtr type, IntPtr[] atoms)
    {
        var data = Marshal.AllocHGlobal(atoms.Length * IntPtr.Size);
        try {
            for (var i = 0; i < atoms.Length; i++)
                Marshal.WriteIntPtr(data, i * IntPtr.Size, atoms[i]);

            XChangeProperty(display, window, property, type, 32, PropModeReplace, data, atoms.Length);
        }
        finally {
            Marshal.FreeHGlobal(data);
        }
    }

    private static void WriteBytes(IntPtr display, IntPtr window, IntPtr property, IntPtr type, byte[] bytes)
    {
        var data = Marshal.AllocHGlobal(bytes.Length);
        try {
            Marshal.Copy(bytes, 0, data, bytes.Length);
            XChangeProperty(display, window, property, type, 8, PropModeReplace, data, bytes.Length);
        }
        finally {
            Marshal.FreeHGlobal(data);
        }
    }

    private static byte[] UriList(string path)
    {
        var uri = new Uri(path).AbsoluteUri;
        return Encoding.ASCII.GetBytes(uri + "\r\n");
    }

    private static byte[] GnomeList(string path)
    {
        var uri = new Uri(path).AbsoluteUri;
        return Encoding.ASCII.GetBytes("copy\n" + uri);
    }

    private static IntPtr Intern(IntPtr display, string name) => XInternAtom(display, name, 0);

    [LibraryImport("libX11.so.6")]
    private static partial int XInitThreads();

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XOpenDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XDefaultScreen(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XRootWindow(IntPtr display, int screen);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint border, UIntPtr borderColor, UIntPtr background);

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr XInternAtom(IntPtr display, string name, int onlyIfExists);

    [LibraryImport("libX11.so.6")]
    private static partial nint XMaxRequestSize(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

    [LibraryImport("libX11.so.6")]
    private static partial int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type, int format, int mode, IntPtr data, int nelements);

    [LibraryImport("libX11.so.6")]
    private static partial int XSendEvent(IntPtr display, IntPtr window, int propagate, IntPtr eventMask, IntPtr ev);

    [LibraryImport("libX11.so.6")]
    private static partial int XFlush(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XPending(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XNextEvent(IntPtr display, IntPtr ev);

    private readonly record struct Offer(string Path, byte[] Png, TaskCompletionSource<bool> Ready);
}