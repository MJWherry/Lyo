using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Grabs a rectangle with BitBlt. The pointer is drawn onto the bitmap when requested.</summary>
[SupportedOSPlatform("windows")]
internal sealed partial class WindowsScreenGrabber : IScreenGrabber
{
    private const uint SrcCopy = 0x00CC0020;
    private const uint DibRgbColors = 0;
    private const int CursorShowing = 1;
    private const int DiNormal = 0x0003;

    public CaptureRegion VirtualScreen => new(GetSystemMetrics(76), GetSystemMetrics(77), GetSystemMetrics(78), GetSystemMetrics(79));

    public Image<Rgba32> Grab(CaptureRegion region, bool includeCursor)
    {
        region = region.ClippedTo(VirtualScreen);
        if (region.Width < 1 || region.Height < 1)
            throw new InvalidOperationException("That rectangle is outside the screen.");

        var screen = GetDC(IntPtr.Zero);
        if (screen == IntPtr.Zero)
            throw new InvalidOperationException("Could not open the desktop device context.");

        var memory = CreateCompatibleDC(screen);
        var bitmap = CreateCompatibleBitmap(screen, region.Width, region.Height);
        var previous = SelectObject(memory, bitmap);
        try {
            if (BitBlt(memory, 0, 0, region.Width, region.Height, screen, region.X, region.Y, SrcCopy) == 0)
                throw new InvalidOperationException("BitBlt failed for that rectangle.");

            if (includeCursor)
                DrawCursor(memory, region);

            var info = new BitmapInfo {
                Header = new() {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = region.Width,
                    Height = -region.Height,
                    Planes = 1,
                    BitCount = 32
                }
            };

            var buffer = new byte[region.Width * region.Height * 4];
            if (GetDIBits(memory, bitmap, 0, (uint)region.Height, buffer, ref info, DibRgbColors) == 0)
                throw new InvalidOperationException("GetDIBits failed for that rectangle.");

            var pixels = new Rgba32[region.Width * region.Height];
            for (var i = 0; i < pixels.Length; i++) {
                var offset = i * 4;
                pixels[i] = new(buffer[offset + 2], buffer[offset + 1], buffer[offset], 255);
            }

            return Image.LoadPixelData(pixels, region.Width, region.Height);
        }
        finally {
            SelectObject(memory, previous);
            DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    public void Dispose() { }

    private static void DrawCursor(IntPtr memory, CaptureRegion region)
    {
        var info = new CursorInfo { Size = Marshal.SizeOf<CursorInfo>() };
        if (GetCursorInfo(ref info) == 0 || info.Flags != CursorShowing || info.Cursor == IntPtr.Zero)
            return;

        DrawIconEx(memory, info.X - region.X, info.Y - region.Y, info.Cursor, 0, 0, 0, IntPtr.Zero, DiNormal);
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetDC(IntPtr window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(IntPtr window, IntPtr dc);

    [LibraryImport("user32.dll")]
    private static partial int GetSystemMetrics(int index);

    [LibraryImport("user32.dll")]
    private static partial int GetCursorInfo(ref CursorInfo info);

    [LibraryImport("user32.dll")]
    private static partial int DrawIconEx(IntPtr dc, int x, int y, IntPtr icon, int width, int height, uint step, IntPtr brush, uint flags);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleDC(IntPtr dc);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr SelectObject(IntPtr dc, IntPtr obj);

    [LibraryImport("gdi32.dll")]
    private static partial int BitBlt(IntPtr dest, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint rop);

    [LibraryImport("gdi32.dll")]
    private static partial int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, byte[] bits, ref BitmapInfo info, uint usage);

    [LibraryImport("gdi32.dll")]
    private static partial int DeleteObject(IntPtr obj);

    [LibraryImport("gdi32.dll")]
    private static partial int DeleteDC(IntPtr dc);

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CursorInfo
    {
        public int Size;
        public int Flags;
        public IntPtr Cursor;
        public int X;
        public int Y;
    }
}