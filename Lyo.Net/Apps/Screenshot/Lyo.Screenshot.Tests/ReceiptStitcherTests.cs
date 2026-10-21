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
}