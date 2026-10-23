using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Lyo.Screenshot.Tests;

public class ReceiptStitcherTests
{
    [Fact]
    public void Stitch_IdenticalFrames_KeepsSingleFrameHeight()
    {
        const int width = 16;
        const int height = 100;
        using var frame = Frames.Rows(width, height, 0);
        using var result = ReceiptStitcher.Stitch([frame, frame, frame]);
        Assert.Equal(height, result.Height);
        Assert.Equal(width, result.Width);
        Frames.AssertRows(result, 0, frame, 0, height);
    }

    [Fact]
    public void Stitch_DownwardShift_AppendsOnlyNewRows()
    {
        const int width = 16;
        const int height = 100;
        const int shift = 40;
        using var first = Frames.Rows(width, height, 0);
        using var second = Frames.Rows(width, height, shift);
        using var result = ReceiptStitcher.Stitch([first, second]);
        Assert.Equal(height + shift, result.Height);
        Assert.Equal(width, result.Width);
        Frames.AssertRows(result, 0, first, 0, height);
        Frames.AssertRows(result, height, second, height - shift, shift);
    }

    [Fact]
    public void Stitch_ThreeDownwardSteps_Accumulates()
    {
        const int width = 16;
        const int height = 100;
        using var first = Frames.Rows(width, height, 0);
        using var second = Frames.Rows(width, height, 30);
        using var third = Frames.Rows(width, height, 80);
        using var result = ReceiptStitcher.Stitch([first, second, third]);
        Assert.Equal(height + 80, result.Height);
        Assert.Equal(width, result.Width);
        for (var y = 0; y < result.Height; y++)
            Assert.Equal(Frames.RowColor(y), result[0, y]);
    }

    [Fact]
    public void Stitch_UpwardShift_PrependsNewRows()
    {
        const int width = 16;
        const int height = 100;
        const int shift = 40;
        using var first = Frames.Rows(width, height, shift);
        using var second = Frames.Rows(width, height, 0);
        using var result = ReceiptStitcher.Stitch([first, second]);
        Assert.Equal(height + shift, result.Height);
        Frames.AssertRows(result, 0, second, 0, shift);
        Frames.AssertRows(result, shift, first, 0, height);
    }

    [Fact]
    public void Stitch_LongDownwardScroll_AppendsEachNewStrip()
    {
        const int width = 24;
        const int height = 50;
        const int shift = 12;
        const int count = 12;
        var frames = new Image<Rgba32>[count];
        try {
            for (var i = 0; i < count; i++)
                frames[i] = Frames.Rows(width, height, i * shift);

            using var result = ReceiptStitcher.Stitch(frames);
            Assert.Equal(height + shift * (count - 1), result.Height);
            for (var y = 0; y < result.Height; y++)
                Assert.Equal(Frames.RowColor(y), result[0, y]);
        }
        finally {
            foreach (var frame in frames)
                frame?.Dispose();
        }
    }

    [Fact]
    public void Stitch_Stream_DownwardShift_MatchesHeldFrames()
    {
        const int width = 16;
        const int height = 100;
        const int shift = 40;
        using var first = Frames.Rows(width, height, 0);
        using var second = Frames.Rows(width, height, shift);
        using var held = ReceiptStitcher.Stitch([first, second]);
        using var stream = new MemoryStream();
        ReceiptStitcher.WriteFrame(stream, first);
        ReceiptStitcher.WriteFrame(stream, second);
        stream.Position = 0;
        using var stitched = ReceiptStitcher.Stitch(stream);
        Assert.Equal(held.Width, stitched.Width);
        Assert.Equal(held.Height, stitched.Height);
        Frames.AssertRows(stitched, 0, held, 0, held.Height);
    }

    [Fact]
    public void Stitch_NoOverlap_AppendsWholeNextFrame()
    {
        const int width = 16;
        const int height = 80;
        using var first = Frames.Rows(width, height, 0);
        using var second = Frames.Flat(width, height, new(255, 0, 128, 255));
        using var result = ReceiptStitcher.Stitch([first, second]);
        Assert.Equal(height * 2, result.Height);
        Frames.AssertRows(result, 0, first, 0, height);
        Frames.AssertRows(result, height, second, 0, height);
    }

    [Fact]
    public void Stitch_ManySmallUpwardSteps_AllocatedBytesStayNearTheImage()
    {
        const int width = 32;
        const int height = 40;
        const int steps = 16;
        var frames = new Image<Rgba32>[steps];
        try {
            for (var i = 0; i < steps; i++)
                frames[i] = Frames.Rows(width, height, steps - 1 - i);

            var before = GC.GetTotalAllocatedBytes(true);
            using var result = ReceiptStitcher.Stitch(frames);
            var allocated = GC.GetTotalAllocatedBytes(true) - before;

            Assert.Equal(height + steps - 1, result.Height);
            Assert.Equal(width, result.Width);
            for (var y = 0; y < result.Height; y++)
                Assert.Equal(Frames.RowColor(y), result[0, y]);

            Assert.True(allocated < 16L * 1024 * 1024, $"allocated {allocated} bytes");
        }
        finally {
            foreach (var frame in frames)
                frame?.Dispose();
        }
    }

    [Fact]
    public void Stitch_StickyHeaderAndBlankFooter_AppendsOnlyNewRows()
    {
        const int width = 96;
        const int height = 100;
        const int shift = 20;
        const int header = 12;
        using var first = DarkPage(width, height, 0, header);
        using var second = DarkPage(width, height, shift, header);
        using var result = ReceiptStitcher.Stitch([first, second]);
        Assert.Equal(height + shift, result.Height);
        Assert.Equal(HeaderColor, result[width / 2, 0]);
        Assert.Equal(Mark(30), result[width / 2, 30]);
        Assert.NotEqual(HeaderColor, result[width / 2, height]);
    }

    [Fact]
    public void Stitch_StickyHeader_UpwardSteps_KeepsOneHeader()
    {
        const int width = 96;
        const int height = 100;
        const int header = 12;
        const int step = 4;
        const int frames = 6;
        const int start = 40;
        var shots = new Image<Rgba32>[frames];
        try {
            for (var i = 0; i < frames; i++)
                shots[i] = DarkPage(width, height, start - i * step, header);

            using var result = ReceiptStitcher.Stitch(shots);
            var lastScroll = start - (frames - 1) * step;
            Assert.Equal(height + step * (frames - 1), result.Height);
            for (var y = 0; y < header; y++)
                Assert.Equal(HeaderColor, result[width / 2, y]);

            Assert.Equal(Mark(lastScroll + header), result[width / 2, header]);
            for (var y = header; y < result.Height; y++)
                Assert.NotEqual(HeaderColor, result[width / 2, y]);
        }
        finally {
            foreach (var shot in shots)
                shot?.Dispose();
        }
    }

    private static Image<Rgba32> DarkPage(int width, int height, int scroll, int header)
    {
        var image = new Image<Rgba32>(width, height, Background);
        for (var y = 0; y < header; y++)
        for (var x = 0; x < width; x++)
            image[x, y] = HeaderColor;

        var contentEnd = height * 3 / 5;
        for (var y = header; y < contentEnd; y++) {
            var color = Mark(scroll + y);
            for (var x = 0; x < width; x++)
                image[x, y] = color;
        }

        return image;
    }

    private static Rgba32 Mark(int row) => new((byte)(row * 13), (byte)(80 + row * 5), (byte)(255 - row * 7), 255);

    private static readonly Rgba32 Background = new(20, 18, 16, 255);
    private static readonly Rgba32 HeaderColor = new(255, 180, 0, 255);
}