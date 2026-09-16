using System.IO.Compression;
using System.Text;

namespace Lyo.Desktop;

/// <summary>Turns an 8-bit PNG into a bottom-up 32-bit DIB for the Win32 clipboard.</summary>
internal static class PngBitmap
{
    internal static byte[] ToDib(byte[] png)
    {
        var image = Decode(png);
        var stride = image.Width * 4;
        var dib = new byte[40 + stride * image.Height];
        WriteInt(dib, 0, 40);
        WriteInt(dib, 4, image.Width);
        WriteInt(dib, 8, image.Height);
        dib[12] = 1;
        dib[14] = 32;
        WriteInt(dib, 20, stride * image.Height);
        for (var y = 0; y < image.Height; y++) {
            var src = (image.Height - 1 - y) * stride;
            image.Bgra.AsSpan(src, stride).CopyTo(dib.AsSpan(40 + y * stride, stride));
        }

        return dib;
    }

    private static Decoded Decode(byte[] png)
    {
        if (png.Length < 8 || png[0] != 137 || png[1] != 80 || png[2] != 78 || png[3] != 71)
            throw new InvalidOperationException("Clipboard image is not a PNG.");

        var width = 0;
        var height = 0;
        var bitDepth = 0;
        var colorType = 0;
        var interlace = 0;
        byte[]? palette = null;
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset + 8 <= png.Length) {
            var length = ReadInt(png, offset);
            if (length < 0 || offset + 12 + length > png.Length)
                throw new InvalidOperationException("PNG chunk is truncated.");
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png.AsSpan(offset + 8, length);
            if (type == "IHDR") {
                width = ReadInt(data, 0);
                height = ReadInt(data, 4);
                bitDepth = data[8];
                colorType = data[9];
                interlace = data[12];
            }
            else if (type == "PLTE") {
                palette = data.ToArray();
            }
            else if (type == "IDAT") {
                idat.Write(data);
            }
            else if (type == "IEND") {
                break;
            }

            offset += 12 + length;
        }

        if (width < 1 || height < 1)
            throw new InvalidOperationException("PNG has no image.");
        if (bitDepth != 8)
            throw new InvalidOperationException("Only 8-bit PNG images can be placed on the clipboard.");
        if (interlace != 0)
            throw new InvalidOperationException("Interlaced PNG images are not supported.");

        var channels = Channels(colorType);
        var raw = Inflate(idat.ToArray());
        var rowBytes = width * channels;
        var expected = (rowBytes + 1) * height;
        if (raw.Length < expected)
            throw new InvalidOperationException("PNG image data is truncated.");

        var bgra = new byte[width * height * 4];
        var previous = new byte[rowBytes];
        var current = new byte[rowBytes];
        var rawOffset = 0;
        for (var y = 0; y < height; y++) {
            var filter = raw[rawOffset];
            rawOffset++;
            Unfilter(raw.AsSpan(rawOffset, rowBytes), current, previous, filter, channels);
            rawOffset += rowBytes;
            Expand(current, bgra.AsSpan(y * width * 4, width * 4), colorType, palette);
            (previous, current) = (current, previous);
        }

        return new(width, height, bgra);
    }

    private static int Channels(int colorType) => colorType switch {
        0 => 1,
        2 => 3,
        3 => 1,
        4 => 2,
        6 => 4,
        _ => throw new InvalidOperationException($"PNG color type {colorType} is not supported.")
    };

    private static byte[] Inflate(byte[] zlib)
    {
        if (zlib.Length < 6)
            throw new InvalidOperationException("PNG image data is truncated.");
        using var input = new MemoryStream(zlib, 2, zlib.Length - 6);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static void Unfilter(ReadOnlySpan<byte> scan, byte[] current, byte[] previous, int filter, int channels)
    {
        for (var i = 0; i < scan.Length; i++) {
            var left = i >= channels ? current[i - channels] : 0;
            var up = previous[i];
            var upperLeft = i >= channels ? previous[i - channels] : 0;
            var value = filter switch {
                0 => scan[i],
                1 => scan[i] + left,
                2 => scan[i] + up,
                3 => scan[i] + ((left + up) / 2),
                4 => scan[i] + Paeth(left, up, upperLeft),
                _ => throw new InvalidOperationException($"PNG filter {filter} is not supported.")
            };
            current[i] = (byte)value;
        }
    }

    private static int Paeth(int left, int up, int upperLeft)
    {
        var estimate = left + up - upperLeft;
        var distanceLeft = Math.Abs(estimate - left);
        var distanceUp = Math.Abs(estimate - up);
        var distanceUpperLeft = Math.Abs(estimate - upperLeft);
        if (distanceLeft <= distanceUp && distanceLeft <= distanceUpperLeft)
            return left;
        return distanceUp <= distanceUpperLeft ? up : upperLeft;
    }

    private static void Expand(byte[] row, Span<byte> bgra, int colorType, byte[]? palette)
    {
        var pixel = 0;
        for (var i = 0; i < row.Length;) {
            byte blue;
            byte green;
            byte red;
            byte alpha = 255;
            if (colorType == 2) {
                red = row[i++];
                green = row[i++];
                blue = row[i++];
            }
            else if (colorType == 6) {
                red = row[i++];
                green = row[i++];
                blue = row[i++];
                alpha = row[i++];
            }
            else if (colorType == 0) {
                blue = green = red = row[i++];
            }
            else if (colorType == 4) {
                blue = green = red = row[i++];
                alpha = row[i++];
            }
            else {
                if (palette is null)
                    throw new InvalidOperationException("PNG palette is missing.");
                var index = row[i++] * 3;
                if (index + 2 >= palette.Length)
                    throw new InvalidOperationException("PNG palette index is out of range.");
                red = palette[index];
                green = palette[index + 1];
                blue = palette[index + 2];
            }

            var at = pixel * 4;
            bgra[at] = blue;
            bgra[at + 1] = green;
            bgra[at + 2] = red;
            bgra[at + 3] = alpha;
            pixel++;
        }
    }

    private static int ReadInt(ReadOnlySpan<byte> data, int offset)
        => (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

    private static int ReadInt(byte[] data, int offset) => ReadInt(data.AsSpan(), offset);

    private static void WriteInt(byte[] data, int offset, int value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    private readonly record struct Decoded(int Width, int Height, byte[] Bgra);
}
