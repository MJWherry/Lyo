using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Grabs a rectangle with XGetImage. Include the pointer by compositing XFixes when libXfixes is present.</summary>
internal sealed partial class X11ScreenGrabber : IScreenGrabber
{
    private const int ZPixmap = 2;
    private readonly Lock _gate = new();
    private IntPtr _display;
    private IntPtr _root;
    private bool _disposed;

    public X11ScreenGrabber()
    {
        XInitThreads();
        _display = XOpenDisplay(IntPtr.Zero);
        if (_display == IntPtr.Zero)
            throw new InvalidOperationException("Could not open the X11 display.");
        var screen = XDefaultScreen(_display);
        _root = XRootWindow(_display, screen);
    }

    public CaptureRegion VirtualScreen
    {
        get
        {
            lock (_gate) {
                ThrowIfDisposed();
                var screen = XDefaultScreen(_display);
                return new(0, 0, XDisplayWidth(_display, screen), XDisplayHeight(_display, screen));
            }
        }
    }

    public Image<Rgba32> Grab(CaptureRegion region, bool includeCursor)
    {
        lock (_gate) {
            ThrowIfDisposed();
            var screen = XDefaultScreen(_display);
            region = region.ClippedTo(new(0, 0, XDisplayWidth(_display, screen), XDisplayHeight(_display, screen)));
            if (region.Width < 1 || region.Height < 1)
                throw new InvalidOperationException("That rectangle is outside the screen.");
            var image = XGetImage(_display, _root, region.X, region.Y, (uint)region.Width, (uint)region.Height, ulong.MaxValue, ZPixmap);
            if (image == IntPtr.Zero)
                throw new InvalidOperationException("XGetImage returned no image for that rectangle.");
            try {
                var native = Marshal.PtrToStructure<XImageNative>(image);
                var grabbed = Read(native, region.Width, region.Height);
                if (includeCursor) {
                    try {
                        CompositeCursor(grabbed, region);
                    }
                    catch (DllNotFoundException) {
                    }
                }
                return grabbed;
            }
            finally {
                Destroy(image);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate) {
            if (_disposed)
                return;
            if (_display != IntPtr.Zero)
                XCloseDisplay(_display);
            _display = IntPtr.Zero;
            _disposed = true;
        }
    }

    private void CompositeCursor(Image<Rgba32> image, CaptureRegion region)
    {
        var cursor = XFixesGetCursorImage(_display);
        if (cursor == IntPtr.Zero)
            return;
        try {
            var header = Marshal.PtrToStructure<XFixesCursorImageNative>(cursor);
            if (header.width == 0 || header.height == 0 || header.pixels == IntPtr.Zero)
                return;
            var count = header.width * header.height;
            var pixels = new ulong[count];
            for (var i = 0; i < count; i++)
                pixels[i] = (ulong)Marshal.ReadInt64(header.pixels, i * 8);
            var left = header.x - region.X - header.xhot;
            var top = header.y - region.Y - header.yhot;
            for (var y = 0; y < header.height; y++) {
                var destY = top + y;
                if (destY < 0 || destY >= image.Height)
                    continue;
                for (var x = 0; x < header.width; x++) {
                    var destX = left + x;
                    if (destX < 0 || destX >= image.Width)
                        continue;
                    var argb = (uint)pixels[(y * header.width) + x];
                    var alpha = (byte)((argb >> 24) & 0xFF);
                    if (alpha == 0)
                        continue;
                    var srcR = Unpremultiply((byte)((argb >> 16) & 0xFF), alpha);
                    var srcG = Unpremultiply((byte)((argb >> 8) & 0xFF), alpha);
                    var srcB = Unpremultiply((byte)(argb & 0xFF), alpha);
                    var dst = image[destX, destY];
                    var inv = (byte)(255 - alpha);
                    image[destX, destY] = new(
                        (byte)((srcR * alpha + dst.R * inv) / 255),
                        (byte)((srcG * alpha + dst.G * inv) / 255),
                        (byte)((srcB * alpha + dst.B * inv) / 255),
                        255);
                }
            }
        }
        finally {
            XFree(cursor);
        }
    }

    private static byte Unpremultiply(byte channel, byte alpha) => alpha is 0 or 255 ? channel : (byte)Math.Min(255, channel * 255 / alpha);

    private static Image<Rgba32> Read(XImageNative native, int width, int height)
    {
        if (native.data == IntPtr.Zero || native.bytes_per_line <= 0 || native.height <= 0)
            throw new InvalidOperationException("XGetImage returned an empty buffer.");
        var stride = native.bytes_per_line;
        var buffer = new byte[stride * native.height];
        Marshal.Copy(native.data, buffer, 0, buffer.Length);
        var redShift = MaskShift(native.red_mask);
        var greenShift = MaskShift(native.green_mask);
        var blueShift = MaskShift(native.blue_mask);
        var bytesPerPixel = Math.Max(1, native.bits_per_pixel / 8);
        var pixels = new Rgba32[width * height];
        var rows = Math.Min(height, native.height);
        var cols = Math.Min(width, native.width);
        for (var y = 0; y < rows; y++) {
            var row = y * stride;
            for (var x = 0; x < cols; x++) {
                var index = row + (x * bytesPerPixel);
                if (index + bytesPerPixel > buffer.Length)
                    break;
                var pixel = ReadPixel(buffer, index, bytesPerPixel, native.byte_order);
                pixels[(y * width) + x] = new(
                    Scale(pixel, redShift, native.red_mask),
                    Scale(pixel, greenShift, native.green_mask),
                    Scale(pixel, blueShift, native.blue_mask),
                    255);
            }
        }

        return Image.LoadPixelData<Rgba32>(pixels, width, height);
    }

    private static ulong ReadPixel(byte[] buffer, int index, int bytesPerPixel, int byteOrder)
    {
        if (bytesPerPixel >= 4) {
            var value = BitConverter.ToUInt32(buffer, index);
            return byteOrder == 1 ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        if (bytesPerPixel == 3)
            return buffer[index] | ((ulong)buffer[index + 1] << 8) | ((ulong)buffer[index + 2] << 16);
        if (bytesPerPixel == 2) {
            var value = BitConverter.ToUInt16(buffer, index);
            return byteOrder == 1 ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        return buffer[index];
    }

    private static int MaskShift(ulong mask)
    {
        var shift = 0;
        while (shift < 64 && (mask & 1UL) == 0) {
            mask >>= 1;
            shift++;
        }

        return shift;
    }

    private static byte Scale(ulong pixel, int shift, ulong mask)
    {
        if (mask == 0)
            return 255;
        var bits = System.Numerics.BitOperations.PopCount(mask);
        var raw = (pixel & mask) >> shift;
        if (bits >= 8)
            return (byte)(raw >> (bits - 8));
        var max = (1UL << bits) - 1;
        return max == 0 ? (byte)0 : (byte)(raw * 255 / max);
    }

    private static void Destroy(IntPtr image)
    {
        var native = Marshal.PtrToStructure<XImageNative>(image);
        if (native.destroy_image == IntPtr.Zero)
            return;
        var destroy = Marshal.GetDelegateForFunctionPointer<DestroyImageDelegate>(native.destroy_image);
        destroy(image);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    [StructLayout(LayoutKind.Sequential)]
    private struct XImageNative
    {
        public int width;
        public int height;
        public int xoffset;
        public int format;
        public IntPtr data;
        public int byte_order;
        public int bitmap_unit;
        public int bitmap_bit_order;
        public int bitmap_pad;
        public int depth;
        public int bytes_per_line;
        public int bits_per_pixel;
        public ulong red_mask;
        public ulong green_mask;
        public ulong blue_mask;
        public IntPtr obdata;
        public IntPtr create_image;
        public IntPtr destroy_image;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XFixesCursorImageNative
    {
        public short x;
        public short y;
        public ushort width;
        public ushort height;
        public ushort xhot;
        public ushort yhot;
        private short pad;
        public ulong cursor_serial;
        public IntPtr pixels;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int DestroyImageDelegate(IntPtr image);

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

    [LibraryImport("libX11.so.6")]
    private static partial int XDisplayWidth(IntPtr display, int screen);

    [LibraryImport("libX11.so.6")]
    private static partial int XDisplayHeight(IntPtr display, int screen);

    [LibraryImport("libX11.so.6")]
    private static partial IntPtr XGetImage(IntPtr display, IntPtr drawable, int x, int y, uint width, uint height, ulong planeMask, int format);

    [LibraryImport("libX11.so.6")]
    private static partial int XFree(IntPtr data);

    [LibraryImport("libXfixes.so.3")]
    private static partial IntPtr XFixesGetCursorImage(IntPtr display);
}
