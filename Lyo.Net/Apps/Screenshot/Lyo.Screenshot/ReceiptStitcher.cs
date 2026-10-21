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
    private const double MatchThreshold = 18;
    private const int SampleRows = 40;
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
    /// canvas, which is where a scroll lands, so a long receipt does not rescan every earlier row. A frame that does not overlap those edges is appended whole. Duplicate frames add
    /// nothing. Callers keep ownership of the inputs.
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
                Prepend(ref pixels, width, ref height, next, above);
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

        var reach = nextHeight * 2;
        var topEnd = Math.Min(last, first + reach);
        var bottomStart = Math.Max(first, last - reach);
        Span<(int Offset, double Score, int Added)> best = stackalloc (int, double, int)[5];
        int found;
        if (bottomStart <= topEnd)
            found = ConsiderRange(canvas, width, height, next, nextHeight, first, last, minOverlap, best, 0);
        else {
            found = ConsiderRange(canvas, width, height, next, nextHeight, first, topEnd, minOverlap, best, 0);
            found = ConsiderRange(canvas, width, height, next, nextHeight, bottomStart, last, minOverlap, best, found);
        }

        if (found == 0)
            return false;

        var bestScore = double.MaxValue;
        var bestAdded = int.MaxValue;
        var chosen = 0;
        var any = false;
        for (var i = 0; i < found; i++) {
            var around = best[i].Offset;
            var from = Math.Max(first, around - 4);
            var to = Math.Min(last, around + 4);
            for (var candidate = from; candidate <= to; candidate++) {
                var score = Score(canvas, width, height, next, nextHeight, candidate, Math.Max(1, width / 48), 1, minOverlap, 120);
                if (score >= MatchThreshold)
                    continue;

                var added = Added(candidate, nextHeight, height);
                if (!any || Better(score, added, bestScore, bestAdded)) {
                    any = true;
                    bestScore = score;
                    bestAdded = added;
                    chosen = candidate;
                }
            }
        }

        offset = chosen;
        return any;
    }

    private static int ConsiderRange(
        Rgba32[] canvas,
        int width,
        int height,
        Rgba32[] next,
        int nextHeight,
        int from,
        int to,
        int minOverlap,
        Span<(int Offset, double Score, int Added)> best,
        int found)
    {
        var stepX = Math.Max(1, width / 24);
        for (var candidate = from; candidate <= to; candidate++) {
            var score = Score(canvas, width, height, next, nextHeight, candidate, stepX, 0, minOverlap, SampleRows);
            if (score >= MatchThreshold)
                continue;

            var added = Added(candidate, nextHeight, height);
            found = Insert(best, found, candidate, score, added);
            if (score == 0 && added == 0)
                break;
        }

        return found;
    }

    private static int Insert(Span<(int Offset, double Score, int Added)> best, int found, int offset, double score, int added)
    {
        var worse = found;
        for (var i = 0; i < found; i++) {
            var current = best[i];
            if (Better(score, added, current.Score, current.Added)) {
                worse = i;
                break;
            }
        }

        if (worse == best.Length)
            return found;

        var last = Math.Min(found, best.Length - 1);
        for (var i = last; i > worse; i--)
            best[i] = best[i - 1];

        best[worse] = (offset, score, added);
        return Math.Min(best.Length, found + 1);
    }

    private static bool Better(double score, int added, double bestScore, int bestAdded) => score < bestScore - 0.05 || (Math.Abs(score - bestScore) <= 0.05 && added < bestAdded);

    private static int Added(int offset, int nextHeight, int canvasHeight) => Math.Max(0, -offset) + Math.Max(0, offset + nextHeight - canvasHeight);

    private static double Score(Rgba32[] canvas, int width, int canvasHeight, Rgba32[] next, int nextHeight, int offset, int stepX, int rowStep, int minOverlap, int maxRows)
    {
        var nextY = Math.Max(0, -offset);
        var canvasY = Math.Max(0, offset);
        var overlap = Math.Min(nextHeight - nextY, canvasHeight - canvasY);
        if (overlap < minOverlap)
            return double.MaxValue;

        var step = rowStep > 0 ? rowStep : Math.Max(1, overlap / maxRows);
        var columns = (width - 1) / stepX + 1;
        var rows = (overlap - 1) / step + 1;
        var planned = columns * rows;
        long sum = 0;
        var count = 0;
        var limit = (long)(MatchThreshold * 3d * planned);
        for (var y = 0; y < overlap; y += step) {
            var nextRow = (nextY + y) * width;
            var canvasRow = (canvasY + y) * width;
            for (var x = 0; x < width; x += stepX) {
                var a = next[nextRow + x];
                var b = canvas[canvasRow + x];
                sum += Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
                count++;
            }

            if (sum > limit)
                return double.MaxValue;
        }

        return count == 0 ? double.MaxValue : sum / (count * 3d);
    }

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

    private static void Prepend(ref Rgba32[] canvas, int width, ref int height, Rgba32[] next, int count)
    {
        var room = MaxHeight - height;
        if (count <= 0 || room <= 0)
            return;

        if (count > room)
            count = room;

        var needed = width * (height + count);
        var grown = new Rgba32[Math.Max(needed, canvas.Length * 2)];
        next.AsSpan(0, count * width).CopyTo(grown);
        canvas.AsSpan(0, width * height).CopyTo(grown.AsSpan(count * width));
        canvas = grown;
        height += count;
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