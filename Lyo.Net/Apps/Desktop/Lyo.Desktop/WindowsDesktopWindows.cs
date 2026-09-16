using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Lyo.Desktop;

/// <summary>Lists visible top-level windows with <c>EnumWindows</c> and raises one with <c>SetForegroundWindow</c>.</summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsDesktopWindows
{
    private const int ExtendedStyle = -20;
    private const int ShowRestore = 9;
    private const nint ToolWindow = 0x80;
    private const uint Owner = 4;
    private static readonly EnumProc Callback = OnWindow;
    private static readonly Lock Gate = new();
    private static List<DesktopWindow>? _found;

    internal static IReadOnlyList<DesktopWindow> List()
    {
        lock (Gate) {
            var found = new List<DesktopWindow>();
            _found = found;
            try {
                EnumWindows(Marshal.GetFunctionPointerForDelegate(Callback), IntPtr.Zero);
                return found;
            }
            finally {
                _found = null;
            }
        }
    }

    internal static void Activate(long id)
    {
        var hwnd = new IntPtr(id);
        if (IsWindow(hwnd) == 0)
            throw new InvalidOperationException("That window is not open.");
        ShowWindow(hwnd, ShowRestore);
        SetForegroundWindow(hwnd);
    }

    private static int OnWindow(IntPtr hwnd, IntPtr lParam)
    {
        if (_found is null || IsWindowVisible(hwnd) == 0 || GetWindow(hwnd, Owner) != IntPtr.Zero)
            return 1;
        if ((GetWindowLongPtr(hwnd, ExtendedStyle) & ToolWindow) != 0)
            return 1;
        if (GetWindowRect(hwnd, out var rect) == 0)
            return 1;
        var name = WindowTitle(hwnd);
        _found.Add(new(hwnd.ToInt64(), name, new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top)));
        return 1;
    }

    private static string WindowTitle(IntPtr hwnd)
    {
        var buffer = Marshal.AllocHGlobal(512 * sizeof(char));
        try {
            var length = GetWindowText(hwnd, buffer, 512);
            return length > 0 ? Marshal.PtrToStringUni(buffer, length)?.Trim() ?? "" : "";
        }
        finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int EnumProc(IntPtr hwnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [LibraryImport("user32.dll")]
    private static partial int EnumWindows(IntPtr callback, IntPtr lParam);

    [LibraryImport("user32.dll")]
    private static partial int IsWindowVisible(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    private static partial int IsWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    private static partial int ShowWindow(IntPtr hwnd, int command);

    [LibraryImport("user32.dll")]
    private static partial int SetForegroundWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetWindow(IntPtr hwnd, uint command);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(IntPtr hwnd, int index);

    [LibraryImport("user32.dll")]
    private static partial int GetWindowRect(IntPtr hwnd, out Rect rect);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
    private static partial int GetWindowText(IntPtr hwnd, IntPtr text, int maxCount);
}
