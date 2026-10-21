using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Lyo.Desktop;

/// <summary>Registers chords with RegisterHotKey on a message-only window.</summary>
[SupportedOSPlatform("windows")]
internal sealed partial class WindowsGlobalHotkeys : IGlobalHotkeys
{
    private const int WmHotkey = 0x0312;
    private const int WmUser = 0x0400;
    private const int WmQuit = 0x0012;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNorepeat = 0x4000;
    private readonly ManualResetEvent _applyDone = new(true);
    private readonly Lock _gate = new();
    private readonly WndProc _proc;
    private readonly ManualResetEvent _ready = new(false);
    private readonly Thread _thread;
    private bool _apply;
    private string? _applyError;
    private HotkeyChord[] _chords = [];
    private Action[] _handlers = [];
    private IntPtr _hwnd;
    private int _registered;
    private HotkeyBinding[] _requested = [];
    private uint _threadId;

    public WindowsGlobalHotkeys()
    {
        _proc = WindowProc;
        _thread = new(Loop) { IsBackground = true, Name = "lyo-hotkeys" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.WaitOne();
        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Could not create the hotkey window.");
    }

    public void Bind(IReadOnlyList<HotkeyBinding> bindings)
    {
        HotkeyBinding.ThrowIfDuplicate(bindings);
        lock (_gate) {
            _requested = [.. bindings];
            _apply = true;
            _applyDone.Reset();
        }

        PostThreadMessage(_threadId, WmUser, IntPtr.Zero, IntPtr.Zero);
        _applyDone.WaitOne();
        if (_applyError != null)
            throw new InvalidOperationException(_applyError);
    }

    public void Dispose()
    {
        if (_threadId != 0)
            PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);

        _thread.Join(TimeSpan.FromSeconds(2));
    }

    private void Loop()
    {
        _threadId = GetCurrentThreadId();
        const string className = "LyoDesktopHotkeys";
        var classNamePtr = Marshal.StringToHGlobalUni(className);
        try {
            var cls = new WndClassEx {
                Size = (uint)Marshal.SizeOf<WndClassEx>(),
                Proc = Marshal.GetFunctionPointerForDelegate(_proc),
                Instance = GetModuleHandle(null),
                ClassName = classNamePtr
            };

            RegisterClassEx(ref cls);
            _hwnd = CreateWindowEx(0, className, "", 0, 0, 0, 0, 0, new(-3), IntPtr.Zero, cls.Instance, IntPtr.Zero);
        }
        finally {
            Marshal.FreeHGlobal(classNamePtr);
        }

        _ready.Set();
        while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0) {
            if (msg.Message == WmUser && _apply)
                ApplyOnThread();

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        if (_hwnd != IntPtr.Zero)
            DestroyWindow(_hwnd);
    }

    private void ApplyOnThread()
    {
        HotkeyBinding[] requested;
        lock (_gate)
            requested = _requested;

        var previousChords = _chords;
        for (var id = 1; id <= _registered; id++)
            UnregisterHotKey(_hwnd, id);

        var handlers = new Action[requested.Length];
        var chords = new HotkeyChord[requested.Length];
        var ok = true;
        for (var i = 0; i < requested.Length; i++) {
            if (!Register(requested[i].Chord, i + 1)) {
                ok = false;
                break;
            }

            handlers[i] = requested[i].Handler;
            chords[i] = requested[i].Chord;
        }

        if (!ok) {
            for (var id = 1; id <= requested.Length; id++)
                UnregisterHotKey(_hwnd, id);

            for (var i = 0; i < previousChords.Length; i++)
                Register(previousChords[i], i + 1);

            _applyError = "That shortcut is already in use.";
        }
        else {
            _handlers = handlers;
            _chords = chords;
            _registered = requested.Length;
            _applyError = null;
        }

        lock (_gate) {
            _apply = false;
            _applyDone.Set();
        }
    }

    private bool Register(HotkeyChord chord, int id)
    {
        var mods = ModNorepeat;
        if (chord.Modifiers.HasFlag(HotkeyModifiers.Alt))
            mods |= ModAlt;

        if (chord.Modifiers.HasFlag(HotkeyModifiers.Ctrl))
            mods |= ModControl;

        if (chord.Modifiers.HasFlag(HotkeyModifiers.Shift))
            mods |= ModShift;

        if (chord.Modifiers.HasFlag(HotkeyModifiers.Meta))
            mods |= ModWin;

        return RegisterHotKey(_hwnd, id, mods, WindowsVirtualKey.Code(chord.Key)) != 0;
    }

    private IntPtr WindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmHotkey) {
            var id = wParam.ToInt32();
            if (id >= 1 && id <= _handlers.Length) {
                try {
                    _handlers[id - 1]();
                }
                catch (Exception) {
                    // A handler must not take down the grab thread.
                }
            }
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }

    [LibraryImport("user32.dll")]
    private static partial ushort RegisterClassEx(ref WndClassEx cls);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr CreateWindowEx(
        uint exStyle,
        string className,
        string windowName,
        uint style,
        int x,
        int y,
        int width,
        int height,
        IntPtr parent,
        IntPtr menu,
        IntPtr instance,
        IntPtr param);

    [LibraryImport("user32.dll")]
    private static partial int DestroyWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    private static partial int GetMessage(out Msg msg, IntPtr hwnd, uint filterMin, uint filterMax);

    [LibraryImport("user32.dll")]
    private static partial int TranslateMessage(ref Msg msg);

    [LibraryImport("user32.dll")]
    private static partial IntPtr DispatchMessage(ref Msg msg);

    [LibraryImport("user32.dll")]
    private static partial IntPtr DefWindowProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial int PostThreadMessage(uint threadId, int msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial int RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);

    [LibraryImport("user32.dll")]
    private static partial int UnregisterHotKey(IntPtr hwnd, int id);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? module);

    private delegate IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct WndClassEx
    {
        public uint Size;
        public uint Style;
        public IntPtr Proc;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public IntPtr MenuName;
        public IntPtr ClassName;
        public IntPtr IconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
    }
}