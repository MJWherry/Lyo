using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Lyo.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot;

/// <summary>Stitches same-width frames into one tall image by matching vertical scroll.</summary>
public static class ReceiptStitcher
{
    private const int MaxHeight = 32768;
    private const int MinRowVotes = 12;
    private static readonly Rgba32[] Empty = [];

    /// <summary>
    /// Appends <paramref name="frame" /> as raw pixels. <see cref="Stitch(Stream)" /> reads one frame, merges it, and drops it before the next. The caller keeps and disposes
    /// <paramref name="frame" />.
    /// </summary>
    public static void WriteFrame(Stream stream, Image<Rgba32> frame)
    {
        OperationHelpers.ThrowIfNotWritable(stream);
        ArgumentHelpers.ThrowIfNull(frame);
        OperationHelpers.ThrowIf(frame.Width <= 0 || frame.Height <= 0, "Receipt frame is empty.");
        Span<byte> header = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(header, frame.Width);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..], frame.Height);
        stream.Write(header);
        frame.ProcessPixelRows(accessor => {
            for (var y = 0; y < accessor.Height; y++)
                stream.Write(MemoryMarshal.AsBytes(accessor.GetRowSpan(y)));
        });

        stream.Flush();
    }

    /// <summary>
    /// Builds one image from <paramref name="frames" /> in order. Downward scroll appends the new rows. Upward scroll prepends them. Matching looks at the top and bottom of the
    /// canvas, which is where a scroll lands, so a long receipt does not rescan every earlier row. Rows that share one flat color, such as a dark page background, do not count as
    /// overlap. A frame that does not overlap those edges is appended whole. Duplicate frames add nothing. Callers keep ownership of the inputs.
    /// </summary>
    public static Image<Rgba32> Stitch(IReadOnlyList<Image<Rgba32>> frames)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(frames);
        var index = 0;
        return Stitch(Read);

        bool Read(out int width, out int height, out Rgba32[] pixels)
        {
            if (index >= frames.Count) {
                width = 0;
                height = 0;
                pixels = Empty;
                return false;
            }

            var image = frames[index++];
            width = image.Width;
            height = image.Height;
            pixels = new Rgba32[width * height];
            image.CopyPixelDataTo(pixels);
            return true;
        }
    }

    /// <summary>Stitches frames written with <see cref="WriteFrame" />. One frame is read from <paramref name="frames" /> and dropped before the next is read.</summary>
    public static Image<Rgba32> Stitch(Stream frames)
    {
        OperationHelpers.ThrowIfNotReadable(frames);
        Rgba32[]? pending = null;
        var transferred = false;
        return Stitch(Read);

        bool Read(out int width, out int height, out Rgba32[] pixels)
        {
            if (!TryReadFrame(frames, ref pending, out width, out height)) {
                pixels = Empty;
                return false;
            }

            pixels = pending!;
            if (!transferred) {
                pending = null;
                transferred = true;
            }

            return true;
        }
    }

    private static Image<Rgba32> Stitch(FramePull pull)
    {
        if (!pull(out var width, out var height, out var pixels))
            throw new InvalidOperationException("No frames were captured.");

        while (pull(out var nextWidth, out var nextHeight, out var next) && height < MaxHeight) {
            OperationHelpers.ThrowIf(nextWidth != width, "Frames in one receipt must share a width.");
            if (!TryFindOffset(pixels, width, height, next, nextHeight, out var offset)) {
                Append(ref pixels, width, ref height, next, 0, nextHeight);
                continue;
            }

            var above = Math.Max(0, -offset);
            var below = Math.Max(0, offset + nextHeight - height);
            if (above == 0 && below == 0)
                continue;

            if (above > 0 && below == 0)
                Prepend(ref pixels, width, ref height, next, nextHeight, above);
            else if (below > 0 && above == 0)
                Append(ref pixels, width, ref height, next, nextHeight - below, below);
            else
                Append(ref pixels, width, ref height, next, 0, nextHeight);
        }

        return Image.LoadPixelData(pixels.AsSpan(0, width * height), width, height);
    }

    private static bool TryReadFrame(Stream stream, ref Rgba32[]? buffer, out int width, out int height)
    {
        Span<byte> header = stackalloc byte[8];
        var read = stream.ReadAtLeast(header, header.Length, false);
        if (read == 0) {
            width = 0;
            height = 0;
            return false;
        }

        OperationHelpers.ThrowIf(read < header.Length, "Receipt frame was cut off.");
        width = BinaryPrimitives.ReadInt32LittleEndian(header);
        height = BinaryPrimitives.ReadInt32LittleEndian(header[4..]);
        OperationHelpers.ThrowIf(width <= 0 || height <= 0, "Receipt frame is empty.");
        var count = (long)width * height;
        OperationHelpers.ThrowIf(count > int.MaxValue, "Receipt frame is too large.");
        var needed = (int)count;
        if (buffer is null || buffer.Length < needed)
            buffer = new Rgba32[needed];

        stream.ReadExactly(MemoryMarshal.AsBytes(buffer.AsSpan(0, needed)));
        return true;
    }

    private static bool TryFindOffset(Rgba32[] canvas, int width, int height, Rgba32[] next, int nextHeight, out int offset)
    {
        var minOverlap = Math.Min(16, nextHeight);
        var first = -(nextHeight - minOverlap);
        var last = height - minOverlap;
        offset = 0;
        if (first > last)
            return false;

        var flat = FlatRow(next, width, nextHeight);
        var index = IndexEdgeRows(canvas, width, height, nextHeight, flat);
        var votes = new Dictionary<int, int>();
        var bestCount = 0;
        for (var y = 0; y < nextHeight; y++) {
            var hash = RowHash(next, width, y);
            if (flat.Ignore && hash == flat.Hash)
                continue;

            if (!index.TryGetValue(hash, out var rows))
                continue;

            foreach (var canvasY in rows) {
                var candidate = canvasY - y;
                if (candidate < first || candidate > last)
                    continue;

                var count = votes.GetValueOrDefault(candidate) + 1;
                votes[candidate] = count;
                if (count > bestCount)
                    bestCount = count;
            }
        }

        if (bestCount < MinRowVotes)
            return false;

        var bestAdded = int.MaxValue;
        var chosen = 0;
        foreach (var pair in votes) {
            if (pair.Value != bestCount)
                continue;

            var added = Added(pair.Key, nextHeight, height);
            if (added >= bestAdded)
                continue;

            bestAdded = added;
            chosen = pair.Key;
        }

        offset = chosen;
        return true;
    }

    private static Dictionary<ulong, List<int>> IndexEdgeRows(Rgba32[] canvas, int width, int height, int nextHeight, (ulong Hash, bool Ignore) flat)
    {
        var index = new Dictionary<ulong, List<int>>();
        var reach = Math.Min(height, nextHeight * 2);
        IndexRows(index, canvas, width, 0, reach, flat);
        if (height > reach * 2)
            IndexRows(index, canvas, width, height - reach, height, flat);
        else if (height > reach)
            IndexRows(index, canvas, width, reach, height, flat);

        return index;
    }

    private static void IndexRows(Dictionary<ulong, List<int>> index, Rgba32[] pixels, int width, int start, int end, (ulong Hash, bool Ignore) flat)
    {
        for (var y = start; y < end; y++) {
            var hash = RowHash(pixels, width, y);
            if (flat.Ignore && hash == flat.Hash)
                continue;

            if (!index.TryGetValue(hash, out var rows)) {
                rows = [];
                index[hash] = rows;
            }

            rows.Add(y);
        }
    }

    private static (ulong Hash, bool Ignore) FlatRow(Rgba32[] pixels, int width, int height)
    {
        var counts = new Dictionary<ulong, int>();
        ulong modal = 0;
        var most = 0;
        for (var y = 0; y < height; y++) {
            var hash = RowHash(pixels, width, y);
            var count = counts.GetValueOrDefault(hash) + 1;
            counts[hash] = count;
            if (count <= most)
                continue;

            most = count;
            modal = hash;
        }

        return (modal, most * 10 > height * 3);
    }

    private static ulong RowHash(Rgba32[] pixels, int width, int y)
    {
        var step = Math.Max(1, Math.Min(8, width / 64));
        ulong hash = 14695981039346656037UL;
        var row = y * width;
        for (var x = 0; x < width; x += step) {
            var pixel = pixels[row + x];
            hash ^= pixel.R;
            hash *= 1099511628211UL;
            hash ^= pixel.G;
            hash *= 1099511628211UL;
            hash ^= pixel.B;
            hash *= 1099511628211UL;
        }

        return hash;
    }

    private static int Added(int offset, int nextHeight, int canvasHeight) => Math.Max(0, -offset) + Math.Max(0, offset + nextHeight - canvasHeight);

    private static void Append(ref Rgba32[] canvas, int width, ref int height, Rgba32[] next, int sourceY, int count)
    {
        var room = MaxHeight - height;
        if (count <= 0 || room <= 0)
            return;

        if (count > room)
            count = room;

        var needed = width * (height + count);
        if (canvas.Length < needed)
            Grow(ref canvas, needed);

        next.AsSpan(sourceY * width, count * width).CopyTo(canvas.AsSpan(height * width));
        height += count;
    }

    private static void Prepend(ref Rgba32[] canvas, int width, ref int height, Rgba32[] next, int nextHeight, int count)
    {
        var room = MaxHeight - height;
        if (count <= 0 || room <= 0)
            return;

        if (count > room)
            count = room;

        var prefix = FixedPrefix(canvas, width, height, next, nextHeight);
        if (prefix + count > nextHeight)
            count = nextHeight - prefix;

        if (count <= 0)
            return;

        var needed = (long)width * (height + count);
        OperationHelpers.ThrowIf(needed > int.MaxValue, "Receipt frame is too large.");
        var neededCount = (int)needed;
        var head = prefix * width;
        var inserted = count * width;
        var tail = (height - prefix) * width;
        if (canvas.Length >= neededCount) {
            for (var i = tail - 1; i >= 0; i--)
                canvas[head + inserted + i] = canvas[head + i];

            next.AsSpan(head, inserted).CopyTo(canvas.AsSpan(head));
            height += count;
            return;
        }

        var size = (long)Math.Max(canvas.Length, 1);
        while (size < neededCount) {
            var nextSize = size + Math.Max(1L, size / 2);
            size = nextSize > int.MaxValue ? neededCount : nextSize;
        }

        var grown = new Rgba32[(int)size];
        canvas.AsSpan(0, head).CopyTo(grown);
        next.AsSpan(head, inserted).CopyTo(grown.AsSpan(head));
        canvas.AsSpan(head, tail).CopyTo(grown.AsSpan(head + inserted));
        canvas = grown;
        height += count;
    }

    private static int FixedPrefix(Rgba32[] canvas, int width, int height, Rgba32[] next, int nextHeight)
    {
        var rows = Math.Min(height, nextHeight);
        var y = 0;
        for (; y < rows; y++) {
            if (RowHash(canvas, width, y) != RowHash(next, width, y))
                break;
        }

        return y;
    }

    private static void Grow(ref Rgba32[] pixels, int needed)
    {
        var size = Math.Max(pixels.Length, 1);
        while (size < needed)
            size = Math.Max(size + 1, size + size / 2);

        Array.Resize(ref pixels, size);
    }

    private delegate bool FramePull(out int width, out int height, out Rgba32[] pixels);
}