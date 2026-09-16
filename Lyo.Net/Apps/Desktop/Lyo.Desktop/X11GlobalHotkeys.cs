using System.Runtime.InteropServices;

namespace Lyo.Desktop;

/// <summary>Grabs chords on the X root window. All X calls stay on one thread.</summary>
internal sealed partial class X11GlobalHotkeys : IGlobalHotkeys
{
    private const int KeyPress = 2;
    private const int GrabModeAsync = 1;
    private const uint AnyModifier = 1u << 15;
    private static int _error;
    private static int _errorRequest;
    private static IntPtr _handlerDisplay;
    private static IntPtr _previousHandler;
    private static readonly XErrorHandler ErrorCallback = OnError;
    private static readonly int ErrorCodeOffset = (int)Marshal.OffsetOf<XErrorEventNative>(nameof(XErrorEventNative.ErrorCode));
    private readonly Lock _gate = new();
    private readonly ManualResetEvent _applyDone = new(true);
    private readonly AutoResetEvent _pulse = new(false);
    private readonly Thread _thread;
    private readonly List<Grabbed> _active = [];
    private List<HotkeyBinding> _requested = [];
    private IntPtr _display;
    private IntPtr _root;
    private bool _apply;
    private bool _running = true;
    private bool _openFailed;
    private string? _applyError;

    public X11GlobalHotkeys()
    {
        _thread = new Thread(Loop) { IsBackground = true, Name = "lyo-hotkeys" };
        _thread.Start();
    }

    /// <summary>Call before any other X11 use, including Avalonia startup. Xlib is not thread-safe until this runs.</summary>
    internal static void InitializeThreading()
    {
        if (!OperatingSystem.IsLinux())
            return;
        XInitThreads();
    }

    public void Bind(IReadOnlyList<HotkeyBinding> bindings)
    {
        HotkeyBinding.ThrowIfDuplicate(bindings);
        lock (_gate) {
            if (_openFailed)
                throw new InvalidOperationException("Could not open the X11 display for hotkeys.");
            _requested = [.. bindings];
            _apply = true;
            _applyDone.Reset();
        }

        _pulse.Set();
        _applyDone.WaitOne();
        if (_applyError != null)
            throw new InvalidOperationException(_applyError);
    }

    public void Dispose()
    {
        _running = false;
        _pulse.Set();
        _thread.Join(TimeSpan.FromSeconds(2));
    }

    private void Loop()
    {
        XInitThreads();
        _display = XOpenDisplay(IntPtr.Zero);
        _handlerDisplay = _display;
        if (_display == IntPtr.Zero) {
            lock (_gate) {
                _openFailed = true;
                _applyError = "Could not open the X11 display for hotkeys.";
                _apply = false;
                _applyDone.Set();
            }

            return;
        }

        _root = XRootWindow(_display, XDefaultScreen(_display));
        while (_running) {
            if (_apply)
                ApplyOnThread();
            Drain();
            _pulse.WaitOne(30);
        }

        UngrabAll();
        XCloseDisplay(_display);
        _handlerDisplay = IntPtr.Zero;
        _display = IntPtr.Zero;
    }

    private void ApplyOnThread()
    {
        List<HotkeyBinding> requested;
        List<Grabbed> previous;
        lock (_gate) {
            requested = _requested;
            previous = [.. _active];
        }

        var failure = WithErrorHandler(() => GrabSet(requested, previous));
        lock (_gate) {
            _applyError = failure;
            _apply = false;
            _applyDone.Set();
        }
    }

    private string? GrabSet(List<HotkeyBinding> requested, List<Grabbed> previous)
    {
        UngrabAll();
        _error = 0;
        var grabbed = new List<Grabbed>(requested.Count);
        foreach (var binding in requested) {
            var code = Grab(binding.Chord);
            XSync(_display, 0);
            if (_error != 0) {
                var message = Describe(_error, _errorRequest);
                UngrabAll();
                Restore(previous);
                return message;
            }

            grabbed.Add(new(code, Modifiers(binding.Chord), binding.Handler));
        }

        _active.Clear();
        _active.AddRange(grabbed);
        return null;
    }

    private void Restore(List<Grabbed> previous)
    {
        _error = 0;
        foreach (var grabbed in previous)
            GrabCode(grabbed.Code, grabbed.Mods);
        XSync(_display, 0);
        _active.Clear();
        _active.AddRange(previous);
    }

    private string? WithErrorHandler(Func<string?> action)
    {
        _error = 0;
        _errorRequest = 0;
        _previousHandler = XSetErrorHandler(Marshal.GetFunctionPointerForDelegate(ErrorCallback));
        try {
            return action();
        }
        catch (Exception ex) {
            return ex.Message;
        }
        finally {
            XSetErrorHandler(_previousHandler);
            _previousHandler = IntPtr.Zero;
        }
    }

    private static string Describe(int code, int request) => code == 10
        ? $"That shortcut is already in use (X11 error {code}, request {request})."
        : $"Could not register the shortcut (X11 error {code}, request {request}).";

    private void Drain()
    {
        while (XPending(_display) > 0) {
            var ev = Marshal.AllocHGlobal(256);
            try {
                XNextEvent(_display, ev);
                var key = Marshal.PtrToStructure<XKeyEventNative>(ev);
                if (key.Type != KeyPress)
                    continue;
                var code = (int)key.KeyCode;
                foreach (var grabbed in _active) {
                    if (grabbed.Code != code || !X11ModifierMatch.Same(key.State, grabbed.Mods))
                        continue;
                    try {
                        grabbed.Handler();
                    }
                    catch (Exception) {
                        // A handler must not take down the grab thread.
                    }

                    break;
                }
            }
            finally {
                Marshal.FreeHGlobal(ev);
            }
        }
    }

    private int Grab(HotkeyChord chord)
    {
        var code = KeyCode(chord.Key);
        GrabCode(code, Modifiers(chord));
        return code;
    }

    private void GrabCode(int code, uint mods)
    {
        uint[] extras = [0, X11ModifierMatch.LockMask, X11ModifierMatch.Mod2Mask, X11ModifierMatch.LockMask | X11ModifierMatch.Mod2Mask];
        foreach (var extra in extras)
            XGrabKey(_display, code, mods | extra, _root, 0, GrabModeAsync, GrabModeAsync);
    }

    private void UngrabAll()
    {
        if (_display == IntPtr.Zero)
            return;
        XUngrabKey(_display, 0, AnyModifier, _root);
    }

    private int KeyCode(string key)
    {
        var sym = XStringToKeysym(key);
        if (sym == 0)
            throw new InvalidOperationException($"Unknown key '{key}'.");
        var code = XKeysymToKeycode(_display, sym);
        if (code == 0)
            throw new InvalidOperationException($"Unknown key '{key}'.");
        return code;
    }

    private static uint Modifiers(HotkeyChord chord)
    {
        uint mods = 0;
        if (chord.Modifiers.HasFlag(HotkeyModifiers.Shift))
            mods |= X11ModifierMatch.ShiftMask;
        if (chord.Modifiers.HasFlag(HotkeyModifiers.Ctrl))
            mods |= X11ModifierMatch.ControlMask;
        if (chord.Modifiers.HasFlag(HotkeyModifiers.Alt))
            mods |= X11ModifierMatch.Mod1Mask;
        if (chord.Modifiers.HasFlag(HotkeyModifiers.Meta))
            mods |= X11ModifierMatch.Mod4Mask;
        return mods;
    }

    private static int OnError(IntPtr display, IntPtr ev)
    {
        var code = Marshal.ReadByte(ev, ErrorCodeOffset);
        var request = Marshal.ReadByte(ev, ErrorCodeOffset + 1);
        if (display == _handlerDisplay) {
            _error = code;
            _errorRequest = request;
            return 0;
        }

        if (_previousHandler == IntPtr.Zero)
            return 0;
        var previous = Marshal.GetDelegateForFunctionPointer<XErrorHandler>(_previousHandler);
        return previous(display, ev);
    }

    private sealed class Grabbed(int code, uint mods, Action handler)
    {
        public int Code { get; } = code;
        public uint Mods { get; } = mods;
        public Action Handler { get; } = handler;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XErrorEventNative
    {
        public int Type;
        public IntPtr Display;
        public IntPtr ResourceId;
        public IntPtr Serial;
        public byte ErrorCode;
        public byte RequestCode;
        public byte MinorCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XKeyEventNative
    {
        public int Type;
        public IntPtr Serial;
        public int SendEvent;
        public IntPtr Display;
        public IntPtr Window;
        public IntPtr Root;
        public IntPtr Subwindow;
        public IntPtr Time;
        public int X;
        public int Y;
        public int XRoot;
        public int YRoot;
        public uint State;
        public uint KeyCode;
        public int SameScreen;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int XErrorHandler(IntPtr display, IntPtr ev);

    [LibraryImport("libX11.so.6")]
    private static partial int XInitThreads();

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XOpenDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XCloseDisplay(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XDefaultScreen(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XRootWindow(IntPtr display, int screen);

    [LibraryImport("libX11.so.6", StringMarshalling = StringMarshalling.Utf8)]
    private static partial ulong XStringToKeysym(string key);

    [LibraryImport("libX11.so.6")]
    private static partial int XKeysymToKeycode(IntPtr display, ulong keysym);

    [LibraryImport("libX11.so.6")]
    private static partial int XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr window, int ownerEvents, int pointerMode, int keyboardMode);

    [LibraryImport("libX11.so.6")]
    private static partial int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr window);

    [LibraryImport("libX11.so.6")]
    private static partial int XSync(IntPtr display, int discard);

    [LibraryImport("libX11.so.6")]
    private static partial int XPending(IntPtr display);

    [LibraryImport("libX11.so.6")]
    private static partial int XNextEvent(IntPtr display, IntPtr ev);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XSetErrorHandler(IntPtr handler);
}
