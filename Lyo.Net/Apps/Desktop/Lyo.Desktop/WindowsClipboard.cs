using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Lyo.Desktop;

/// <summary>Win32 clipboard. Text reads and PNG writes. Text writes stay on <c>clip.exe</c>.</summary>
[SupportedOSPlatform("windows")]
internal static partial class WindowsClipboard
{
    private const uint CfDib = 8;
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    internal static string GetText()
    {
        Open();
        try {
            var handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero)
                return "";
            var locked = GlobalLock(handle);
            if (locked == IntPtr.Zero)
                return "";
            try {
                return Marshal.PtrToStringUni(locked) ?? "";
            }
            finally {
                GlobalUnlock(handle);
            }
        }
        finally {
            CloseClipboard();
        }
    }

    internal static void SetPng(byte[] png)
    {
        var format = RegisterClipboardFormat("PNG");
        if (format == 0)
            throw new InvalidOperationException("Could not register the PNG clipboard format.");
        var dib = PngBitmap.ToDib(png);
        Open();
        try {
            if (EmptyClipboard() == 0)
                throw new InvalidOperationException("Could not empty the clipboard.");
            Give(format, png);
            Give(CfDib, dib);
        }
        finally {
            CloseClipboard();
        }
    }

    private static void Open()
    {
        if (OpenClipboard(IntPtr.Zero) == 0)
            throw new InvalidOperationException("Could not open the clipboard.");
    }

    private static void Give(uint format, byte[] bytes)
    {
        var memory = GlobalAlloc(GmemMoveable, (nuint)bytes.Length);
        if (memory == IntPtr.Zero)
            throw new InvalidOperationException("Could not allocate clipboard memory.");
        var locked = GlobalLock(memory);
        if (locked == IntPtr.Zero) {
            GlobalFree(memory);
            throw new InvalidOperationException("Could not lock clipboard memory.");
        }

        Marshal.Copy(bytes, 0, locked, bytes.Length);
        GlobalUnlock(memory);
        if (SetClipboardData(format, memory) != IntPtr.Zero)
            return;
        GlobalFree(memory);
        throw new InvalidOperationException("Could not set clipboard data.");
    }

    [LibraryImport("user32.dll")]
    private static partial int OpenClipboard(IntPtr owner);

    [LibraryImport("user32.dll")]
    private static partial int CloseClipboard();

    [LibraryImport("user32.dll")]
    private static partial int EmptyClipboard();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint RegisterClipboardFormat(string format);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetClipboardData(uint format);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SetClipboardData(uint format, IntPtr memory);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalAlloc(uint flags, nuint bytes);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalLock(IntPtr memory);

    [LibraryImport("kernel32.dll")]
    private static partial int GlobalUnlock(IntPtr memory);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GlobalFree(IntPtr memory);
}
