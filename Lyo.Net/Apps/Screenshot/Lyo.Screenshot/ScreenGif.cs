using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>
/// Writes a looping GIF one frame at a time. Each frame is reduced to 256 colors with a small histogram, then LZW-compressed, so a large frame is not quantized by
/// ImageSharp.
/// </summary>
internal static class ScreenGif
{
    internal const byte Trailer = 0x3B;

    private const int Shift = 3;
    private const int Bins = 32 * 32 * 32;
    private const int ClearCode = 256;
    private const int EndCode = 257;

    private static readonly byte[] LoopForever = [
        0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50,
        0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00
    ];

    internal static void WriteFrame(Stream output, Image<Rgba32> image, int delayCentiseconds, bool first)
    {
        var indices = Index(image, out var palette);
        if (first)
            WriteHeader(output, image.Width, image.Height);

        WriteGraphicControl(output, delayCentiseconds);
        WriteImage(output, image.Width, image.Height, palette, indices);
    }

    private static byte[] Index(Image<Rgba32> image, out Rgba32[] palette)
    {
        var counts = new int[Bins];
        image.ProcessPixelRows(accessor => {
            for (var y = 0; y < accessor.Height; y++) {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    counts[Key(row[x])]++;
            }
        });

        var ranked = new int[Bins];
        for (var i = 0; i < Bins; i++)
            ranked[i] = i;

        Array.Sort(ranked, (left, right) => counts[right].CompareTo(counts[left]));
        var used = 0;
        while (used < 256 && counts[ranked[used]] > 0)
            used++;

        if (used == 0)
            used = 1;

        palette = new Rgba32[256];
        var chosen = new bool[Bins];
        var binIndex = new byte[Bins];
        for (var i = 0; i < used; i++) {
            var key = ranked[i];
            chosen[key] = true;
            binIndex[key] = (byte)i;
            palette[i] = Color(key);
        }

        for (var bin = 0; bin < Bins; bin++) {
            if (counts[bin] == 0 || chosen[bin])
                continue;

            binIndex[bin] = Nearest(bin, palette, used);
        }

        var indices = new byte[image.Width * image.Height];
        var offset = 0;
        image.ProcessPixelRows(accessor => {
            for (var y = 0; y < accessor.Height; y++) {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    indices[offset++] = binIndex[Key(row[x])];
            }
        });

        return indices;
    }

    private static int Key(Rgba32 pixel) => ((pixel.R >> Shift) << 10) | ((pixel.G >> Shift) << 5) | (pixel.B >> Shift);

    private static Rgba32 Color(int key)
    {
        var red = (key >> 10) & 31;
        var green = (key >> 5) & 31;
        var blue = key & 31;
        return new((byte)((red << 3) | (red >> 2)), (byte)((green << 3) | (green >> 2)), (byte)((blue << 3) | (blue >> 2)), 255);
    }

    private static byte Nearest(int key, Rgba32[] palette, int used)
    {
        var red = (key >> 10) & 31;
        var green = (key >> 5) & 31;
        var blue = key & 31;
        var best = 0;
        var bestDistance = int.MaxValue;
        for (var i = 0; i < used; i++) {
            var deltaRed = red - (palette[i].R >> Shift);
            var deltaGreen = green - (palette[i].G >> Shift);
            var deltaBlue = blue - (palette[i].B >> Shift);
            var distance = deltaRed * deltaRed + deltaGreen * deltaGreen + deltaBlue * deltaBlue;
            if (distance >= bestDistance)
                continue;

            best = i;
            bestDistance = distance;
            if (distance == 0)
                break;
        }

        return (byte)best;
    }

    private static void WriteHeader(Stream output, int width, int height)
    {
        output.Write("GIF89a"u8);
        WriteShort(output, width);
        WriteShort(output, height);
        output.WriteByte(0x70);
        output.WriteByte(0);
        output.WriteByte(0);
        output.Write(LoopForever);
    }

    private static void WriteGraphicControl(Stream output, int delayCentiseconds)
    {
        output.WriteByte(0x21);
        output.WriteByte(0xF9);
        output.WriteByte(0x04);
        output.WriteByte(0x04);
        WriteShort(output, delayCentiseconds);
        output.WriteByte(0);
        output.WriteByte(0);
    }

    private static void WriteImage(Stream output, int width, int height, Rgba32[] palette, byte[] indices)
    {
        output.WriteByte(0x2C);
        WriteShort(output, 0);
        WriteShort(output, 0);
        WriteShort(output, width);
        WriteShort(output, height);
        output.WriteByte(0x87);
        foreach (var color in palette) {
            output.WriteByte(color.R);
            output.WriteByte(color.G);
            output.WriteByte(color.B);
        }

        var lzw = new LzwWriter(output);
        lzw.Start();
        foreach (var index in indices)
            lzw.WriteIndex(index);

        lzw.Finish();
    }

    private static void WriteShort(Stream output, int value)
    {
        output.WriteByte((byte)value);
        output.WriteByte((byte)(value >> 8));
    }

    private sealed class LzwWriter(Stream output)
    {
        private readonly byte[] _block = new byte[255];
        private readonly Dictionary<int, int> _codes = new();
        private int _bitCount;
        private int _bits;
        private int _blockLength;
        private int _codeSize = 9;
        private int _nextCode = EndCode + 1;
        private int _prefix = -1;

        internal void Start()
        {
            output.WriteByte(8);
            WriteCode(ClearCode);
        }

        internal void WriteIndex(byte index)
        {
            if (_prefix < 0) {
                _prefix = index;
                return;
            }

            var key = (_prefix << 8) | index;
            if (_codes.TryGetValue(key, out var code)) {
                _prefix = code;
                return;
            }

            WriteCode(_prefix);
            if (_nextCode < 4096) {
                _codes[key] = _nextCode;
                _nextCode++;
            }
            else {
                WriteCode(ClearCode);
                _codes.Clear();
                _nextCode = EndCode + 1;
                _codeSize = 9;
            }

            _prefix = index;
        }

        internal void Finish()
        {
            if (_prefix >= 0)
                WriteCode(_prefix);

            WriteCode(EndCode);
            if (_bitCount > 0)
                Emit((byte)_bits);

            if (_blockLength > 0) {
                output.WriteByte((byte)_blockLength);
                output.Write(_block, 0, _blockLength);
            }

            output.WriteByte(0);
        }

        private void WriteCode(int code)
        {
            _bits |= code << _bitCount;
            _bitCount += _codeSize;
            while (_bitCount >= 8) {
                Emit((byte)_bits);
                _bits >>= 8;
                _bitCount -= 8;
            }

            // The code just written still uses the old width. The next code grows once the table passes 2^width - 1.
            if (_codeSize < 12 && _nextCode > (1 << _codeSize) - 1)
                _codeSize++;
        }

        private void Emit(byte value)
        {
            _block[_blockLength++] = value;
            if (_blockLength < 255)
                return;

            output.WriteByte(255);
            output.Write(_block, 0, 255);
            _blockLength = 0;
        }
    }
}